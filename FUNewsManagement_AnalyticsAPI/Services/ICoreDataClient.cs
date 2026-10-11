using FUNewsManagement_AnalyticsAPI.Models;

namespace FUNewsManagement_AnalyticsAPI.Services;

public interface ICoreDataClient
{
    Task<List<CoreNewsArticleDto>> GetAllNewsArticlesAsync(CancellationToken cancellationToken = default);
    Task<List<CoreNewsArticleDto>> GetActiveNewsArticlesAsync(CancellationToken cancellationToken = default);
    Task<CoreNewsArticleDto?> GetNewsArticleByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<List<CoreCategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetViewStatsAsync(CancellationToken cancellationToken = default);
}
