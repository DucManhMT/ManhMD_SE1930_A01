using FUNewsManagement_AIAPI.Models;

namespace FUNewsManagement_AIAPI.Services;

public interface ITagCatalogClient
{
    Task<List<CoreTagDto>> GetAvailableTagsAsync(CancellationToken cancellationToken = default);
}
