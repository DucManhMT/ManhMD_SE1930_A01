using FUNews.DataAccess.Entities;

namespace FUNews.DataAccess.Repositories;

public interface ITagRepository
{
    IQueryable<Tag> GetQueryable();
    Task<Tag?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Tag>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
    Task<List<Tag>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IsNameUniqueAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasArticlesAsync(int id, CancellationToken cancellationToken = default);
    Task<Tag?> GetByIdWithNewsTagsAsync(int id, bool asNoTracking = false, CancellationToken cancellationToken = default);
    Task<List<NewsArticle>> GetArticlesByTagAsync(int tagId, bool? activeOnly = null, CancellationToken cancellationToken = default);
    Task<Tag> AddAsync(Tag tag, CancellationToken cancellationToken = default);
    Task<Tag> UpdateAsync(Tag tag, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
