using FUNews.Client.DataAccess.Clients;
using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.BusinessLogic.Services;

public class NewsClientService : INewsClientService
{
    private readonly IFUNewsApiClient _apiClient;

    public NewsClientService(IFUNewsApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public Task<ODataEnvelope<NewsArticleApiModel>> GetNewsArticlesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
    {
        return _apiClient.GetNewsArticlesAsync(odataQuery, cancellationToken);
    }

    public Task<NewsArticleApiModel?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return _apiClient.GetNewsArticleByIdAsync(id, cancellationToken);
    }

    public Task<NewsArticleApiModel> CreateNewsArticleAsync(CreateNewsArticleApiModel request, CancellationToken cancellationToken = default)
    {
        return _apiClient.CreateNewsArticleAsync(request, cancellationToken);
    }

    public Task<NewsArticleApiModel> UpdateNewsArticleAsync(string id, UpdateNewsArticleApiModel request, CancellationToken cancellationToken = default)
    {
        return _apiClient.UpdateNewsArticleAsync(id, request, cancellationToken);
    }

    public Task DeleteNewsArticleAsync(string id, CancellationToken cancellationToken = default)
    {
        return _apiClient.DeleteNewsArticleAsync(id, cancellationToken);
    }

    public Task<NewsArticleApiModel> DuplicateNewsArticleAsync(string id, CancellationToken cancellationToken = default)
    {
        return _apiClient.DuplicateNewsArticleAsync(id, cancellationToken);
    }
}

