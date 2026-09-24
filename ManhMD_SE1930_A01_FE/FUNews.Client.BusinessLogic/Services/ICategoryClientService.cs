using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.BusinessLogic.Services;

public interface ICategoryClientService
{
    Task<ODataEnvelope<CategoryApiModel>> GetCategoriesAsync(string? odataQuery = null, CancellationToken cancellationToken = default);
    Task<CategoryApiModel?> GetByIdAsync(short id, CancellationToken cancellationToken = default);
    Task<CategoryApiModel> CreateCategoryAsync(CreateCategoryApiModel request, CancellationToken cancellationToken = default);
}
