using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.BusinessLogic.Services;

public interface ITagClientService
{
    Task<ODataEnvelope<TagApiModel>> GetTagsAsync(string? odataQuery = null, CancellationToken cancellationToken = default);
    Task<TagApiModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
