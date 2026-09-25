using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.BusinessLogic.Services;

public interface INewsClientService
{
    Task<ODataEnvelope<NewsArticleApiModel>> GetNewsArticlesAsync(string? odataQuery = null, CancellationToken cancellationToken = default);
    Task<NewsArticleApiModel?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<NewsArticleApiModel> CreateNewsArticleAsync(CreateNewsArticleApiModel request, CancellationToken cancellationToken = default);
    Task<NewsArticleApiModel> UpdateNewsArticleAsync(string id, UpdateNewsArticleApiModel request, CancellationToken cancellationToken = default);
    Task DeleteNewsArticleAsync(string id, CancellationToken cancellationToken = default);
    Task<NewsArticleApiModel> DuplicateNewsArticleAsync(string id, CancellationToken cancellationToken = default);
    Task<ODataEnvelope<NewsArticleApiModel>> GetMyNewsArticlesAsync(string? odataQuery = null, CancellationToken cancellationToken = default);
}

