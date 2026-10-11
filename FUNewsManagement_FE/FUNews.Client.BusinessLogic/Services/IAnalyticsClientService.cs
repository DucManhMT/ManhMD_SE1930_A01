using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.BusinessLogic.Services;

public interface IAnalyticsClientService
{
    Task<AnalyticsDashboardApiModel> GetDashboardAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        short? categoryId = null,
        bool? status = null,
        CancellationToken cancellationToken = default);

    Task<List<TrendingArticleApiModel>> GetTrendingArticlesAsync(
        int limit = 10,
        CancellationToken cancellationToken = default);

    Task<List<RecommendedArticleApiModel>> GetRecommendedArticlesAsync(
        string articleId,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportExcelAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        short? categoryId = null,
        CancellationToken cancellationToken = default);
}
