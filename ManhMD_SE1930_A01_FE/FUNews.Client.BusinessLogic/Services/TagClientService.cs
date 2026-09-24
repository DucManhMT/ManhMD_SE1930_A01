using FUNews.Client.DataAccess.Clients;
using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.BusinessLogic.Services;

public class TagClientService : ITagClientService
{
    private readonly IFUNewsApiClient _apiClient;

    public TagClientService(IFUNewsApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public Task<ODataEnvelope<TagApiModel>> GetTagsAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
    {
        return _apiClient.GetTagsAsync(odataQuery, cancellationToken);
    }

    public Task<TagApiModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _apiClient.GetTagByIdAsync(id, cancellationToken);
    }

    public Task<TagApiModel> CreateTagAsync(CreateTagApiModel request, CancellationToken cancellationToken = default)
    {
        return _apiClient.CreateTagAsync(request, cancellationToken);
    }

    public Task<TagApiModel> UpdateTagAsync(int id, UpdateTagApiModel request, CancellationToken cancellationToken = default)
    {
        return _apiClient.UpdateTagAsync(id, request, cancellationToken);
    }

    public Task DeleteTagAsync(int id, CancellationToken cancellationToken = default)
    {
        return _apiClient.DeleteTagAsync(id, cancellationToken);
    }

    public Task<List<NewsArticleApiModel>> GetArticlesByTagAsync(int tagId, CancellationToken cancellationToken = default)
    {
        return _apiClient.GetNewsArticlesByTagAsync(tagId, cancellationToken);
    }
}
