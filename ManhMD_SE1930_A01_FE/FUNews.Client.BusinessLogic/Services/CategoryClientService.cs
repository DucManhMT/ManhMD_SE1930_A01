using FUNews.Client.DataAccess.Clients;
using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.BusinessLogic.Services;

public class CategoryClientService : ICategoryClientService
{
    private readonly IFUNewsApiClient _apiClient;

    public CategoryClientService(IFUNewsApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public Task<ODataEnvelope<CategoryApiModel>> GetCategoriesAsync(string? odataQuery = null, CancellationToken cancellationToken = default)
    {
        return _apiClient.GetCategoriesAsync(odataQuery, cancellationToken);
    }

    public Task<CategoryApiModel?> GetByIdAsync(short id, CancellationToken cancellationToken = default)
    {
        return _apiClient.GetCategoryByIdAsync(id, cancellationToken);
    }

    public Task<CategoryApiModel> CreateCategoryAsync(CreateCategoryApiModel request, CancellationToken cancellationToken = default)
    {
        return _apiClient.CreateCategoryAsync(request, cancellationToken);
    }

    public Task<CategoryApiModel> UpdateCategoryAsync(short id, UpdateCategoryApiModel request, CancellationToken cancellationToken = default)
    {
        return _apiClient.UpdateCategoryAsync(id, request, cancellationToken);
    }

    public Task DeleteCategoryAsync(short id, CancellationToken cancellationToken = default)
    {
        return _apiClient.DeleteCategoryAsync(id, cancellationToken);
    }
}
