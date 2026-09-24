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
}
