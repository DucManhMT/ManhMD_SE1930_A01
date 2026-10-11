using FUNewsManagement_AnalyticsAPI.Models;

namespace FUNewsManagement_AnalyticsAPI.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ICoreDataClient _coreDataClient;
    private readonly IExcelExportService _excelExportService;

    public AnalyticsService(ICoreDataClient coreDataClient, IExcelExportService excelExportService)
    {
        _coreDataClient = coreDataClient ?? throw new ArgumentNullException(nameof(coreDataClient));
        _excelExportService = excelExportService ?? throw new ArgumentNullException(nameof(excelExportService));
    }

    public async Task<AnalyticsDashboardDto> GetDashboardStatsAsync(DashboardFilterQuery filter, CancellationToken cancellationToken = default)
    {
        var articles = await _coreDataClient.GetAllNewsArticlesAsync(cancellationToken);
        var categories = await _coreDataClient.GetAllCategoriesAsync(cancellationToken);

        var filtered = FilterArticles(articles, filter);

        var categoryMap = categories.ToDictionary(c => c.CategoryId, c => c.CategoryName);
        var activeCount = filtered.Count(a => a.NewsStatus == true);
        var inactiveCount = filtered.Count(a => a.NewsStatus == false);

        // Group by Category
        var byCategory = filtered
            .Where(a => a.CategoryId.HasValue)
            .GroupBy(a => a.CategoryId!.Value)
            .Select(g => new CategoryStatItemDto
            {
                CategoryId = g.Key,
                CategoryName = categoryMap.TryGetValue(g.Key, out var name) ? name : (g.First().CategoryName ?? $"Category #{g.Key}"),
                Count = g.Count()
            })
            .OrderByDescending(c => c.Count)
            .ToList();

        // Group by Status
        var byStatus = new List<StatusStatItemDto>
        {
            new() { Status = "Active", Count = activeCount },
            new() { Status = "Inactive", Count = inactiveCount }
        };

        // Group by Period (Month: yyyy-MM)
        var byPeriod = filtered
            .Where(a => a.CreatedDate.HasValue)
            .GroupBy(a => a.CreatedDate!.Value.ToString("yyyy-MM"))
            .OrderBy(g => g.Key)
            .Select(g => new PeriodStatItemDto
            {
                Period = g.Key,
                Count = g.Count()
            })
            .ToList();

        return new AnalyticsDashboardDto
        {
            TotalArticles = filtered.Count,
            ActiveArticles = activeCount,
            InactiveArticles = inactiveCount,
            ArticlesByCategory = byCategory,
            ByStatus = byStatus,
            ArticlesByPeriod = byPeriod
        };
    }

    public async Task<List<TrendingArticleDto>> GetTrendingArticlesAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        if (limit <= 0) limit = 10;
        if (limit > 100) limit = 100;

        var articles = await _coreDataClient.GetAllNewsArticlesAsync(cancellationToken);
        var viewStats = await _coreDataClient.GetViewStatsAsync(cancellationToken);

        var trending = articles
            .Select(a => new TrendingArticleDto
            {
                NewsArticleId = a.NewsArticleId,
                NewsTitle = a.NewsTitle ?? string.Empty,
                ViewCount = viewStats.TryGetValue(a.NewsArticleId, out var count) ? count : 0,
                CreatedDate = a.CreatedDate,
                CategoryName = a.CategoryName
            })
            .OrderByDescending(a => a.ViewCount)
            .ThenByDescending(a => a.CreatedDate)
            .Take(limit)
            .ToList();

        return trending;
    }

    public async Task<List<RecommendedArticleDto>?> GetRecommendedArticlesAsync(string articleId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(articleId))
        {
            return null;
        }

        var target = await _coreDataClient.GetNewsArticleByIdAsync(articleId, cancellationToken);
        if (target == null)
        {
            return null;
        }

        var activeArticles = await _coreDataClient.GetActiveNewsArticlesAsync(cancellationToken);
        var targetTagIds = target.Tags?.Select(t => t.TagId).ToHashSet() ?? new HashSet<int>();

        var recommended = activeArticles
            .Where(a => !string.Equals(a.NewsArticleId, articleId, StringComparison.OrdinalIgnoreCase))
            .Where(a => a.NewsStatus == true)
            .Select(a =>
            {
                int commonTags = a.Tags?.Count(t => targetTagIds.Contains(t.TagId)) ?? 0;
                bool sameCategory = target.CategoryId.HasValue && a.CategoryId.HasValue && target.CategoryId == a.CategoryId;
                int score = (sameCategory ? 3 : 0) + (commonTags * 2);

                return new
                {
                    Article = a,
                    Score = score,
                    HasRelation = sameCategory || commonTags > 0
                };
            })
            .Where(x => x.HasRelation)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Article.CreatedDate)
            .Take(3)
            .Select(x => new RecommendedArticleDto
            {
                NewsArticleId = x.Article.NewsArticleId,
                NewsTitle = x.Article.NewsTitle ?? string.Empty,
                Headline = x.Article.Headline,
                CategoryName = x.Article.CategoryName
            })
            .ToList();

        return recommended;
    }

    public async Task<byte[]> ExportExcelReportAsync(DashboardFilterQuery filter, CancellationToken cancellationToken = default)
    {
        var articles = await _coreDataClient.GetAllNewsArticlesAsync(cancellationToken);
        var viewStats = await _coreDataClient.GetViewStatsAsync(cancellationToken);
        var dashboard = await GetDashboardStatsAsync(filter, cancellationToken);
        var filteredArticles = FilterArticles(articles, filter);

        return _excelExportService.ExportDashboardReport(dashboard, filteredArticles, viewStats, filter);
    }

    private static List<CoreNewsArticleDto> FilterArticles(List<CoreNewsArticleDto> articles, DashboardFilterQuery filter)
    {
        var query = articles.AsEnumerable();

        if (filter.StartDate.HasValue)
        {
            query = query.Where(a => a.CreatedDate >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            var endOfDay = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(a => a.CreatedDate <= endOfDay);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(a => a.CategoryId == filter.CategoryId.Value);
        }

        if (filter.AuthorId.HasValue)
        {
            query = query.Where(a => a.CreatedById == filter.AuthorId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(a => a.NewsStatus == filter.Status.Value);
        }

        return query.ToList();
    }
}
