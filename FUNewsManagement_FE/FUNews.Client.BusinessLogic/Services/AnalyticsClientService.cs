using FUNews.Client.DataAccess.Clients;
using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.BusinessLogic.Services;

public class AnalyticsClientService : IAnalyticsClientService
{
    private readonly IAnalyticsApiClient _analyticsApiClient;

    public AnalyticsClientService(IAnalyticsApiClient analyticsApiClient)
    {
        _analyticsApiClient = analyticsApiClient ?? throw new ArgumentNullException(nameof(analyticsApiClient));
    }

    public Task<AnalyticsDashboardApiModel> GetDashboardAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        short? categoryId = null,
        bool? status = null,
        CancellationToken cancellationToken = default)
    {
        return _analyticsApiClient.GetDashboardAsync(startDate, endDate, categoryId, status, cancellationToken);
    }

    public Task<List<TrendingArticleApiModel>> GetTrendingArticlesAsync(
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        return _analyticsApiClient.GetTrendingArticlesAsync(limit, cancellationToken);
    }

    public Task<List<RecommendedArticleApiModel>> GetRecommendedArticlesAsync(
        string articleId,
        CancellationToken cancellationToken = default)
    {
        return _analyticsApiClient.GetRecommendedArticlesAsync(articleId, cancellationToken);
    }

    public Task<byte[]> ExportExcelAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        short? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        return _analyticsApiClient.ExportExcelAsync(startDate, endDate, categoryId, cancellationToken);
    }
}
