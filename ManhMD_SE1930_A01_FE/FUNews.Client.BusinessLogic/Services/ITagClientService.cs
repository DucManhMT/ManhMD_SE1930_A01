using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.BusinessLogic.Services;

public interface ITagClientService
{
    Task<ODataEnvelope<TagApiModel>> GetTagsAsync(string? odataQuery = null, CancellationToken cancellationToken = default);
    Task<TagApiModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TagApiModel> CreateTagAsync(CreateTagApiModel request, CancellationToken cancellationToken = default);
    Task<TagApiModel> UpdateTagAsync(int id, UpdateTagApiModel request, CancellationToken cancellationToken = default);
    Task DeleteTagAsync(int id, CancellationToken cancellationToken = default);
    Task<List<NewsArticleApiModel>> GetArticlesByTagAsync(int tagId, CancellationToken cancellationToken = default);
}
