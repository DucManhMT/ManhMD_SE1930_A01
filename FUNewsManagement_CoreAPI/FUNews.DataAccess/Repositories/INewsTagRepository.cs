using FUNews.DataAccess.Entities;

namespace FUNews.DataAccess.Repositories;

public interface INewsTagRepository
{
    Task<List<NewsTag>> GetByArticleIdAsync(string articleId, CancellationToken cancellationToken = default);
    Task<List<NewsTag>> GetByTagIdAsync(int tagId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<NewsTag> newsTags, CancellationToken cancellationToken = default);
    Task RemoveRangeAsync(IEnumerable<NewsTag> newsTags, CancellationToken cancellationToken = default);
    Task ReplaceTagsForArticleAsync(string articleId, IEnumerable<int> tagIds, CancellationToken cancellationToken = default);
    Task DeleteByArticleIdAsync(string articleId, CancellationToken cancellationToken = default);
}
