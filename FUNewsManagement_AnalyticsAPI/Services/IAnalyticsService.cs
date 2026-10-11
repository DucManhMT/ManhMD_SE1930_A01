using FUNewsManagement_AnalyticsAPI.Models;

namespace FUNewsManagement_AnalyticsAPI.Services;

public interface IAnalyticsService
{
    Task<AnalyticsDashboardDto> GetDashboardStatsAsync(DashboardFilterQuery filter, CancellationToken cancellationToken = default);
    Task<List<TrendingArticleDto>> GetTrendingArticlesAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task<List<RecommendedArticleDto>?> GetRecommendedArticlesAsync(string articleId, CancellationToken cancellationToken = default);
    Task<byte[]> ExportExcelReportAsync(DashboardFilterQuery filter, CancellationToken cancellationToken = default);
}
