using FUNews.DataAccess.DAOs;
using FUNews.DataAccess.Entities;

namespace FUNews.DataAccess.Repositories;

public class NewsTagRepository : INewsTagRepository
{
    private readonly NewsTagDAO _newsTagDao;

    public NewsTagRepository(NewsTagDAO newsTagDao)
    {
        _newsTagDao = newsTagDao ?? throw new ArgumentNullException(nameof(newsTagDao));
    }

    public Task<List<NewsTag>> GetByArticleIdAsync(string articleId, CancellationToken cancellationToken = default)
    {
        return _newsTagDao.GetByArticleIdAsync(articleId, cancellationToken);
    }

    public Task<List<NewsTag>> GetByTagIdAsync(int tagId, CancellationToken cancellationToken = default)
    {
        return _newsTagDao.GetByTagIdAsync(tagId, cancellationToken);
    }

    public Task AddRangeAsync(IEnumerable<NewsTag> newsTags, CancellationToken cancellationToken = default)
    {
        return _newsTagDao.AddRangeAsync(newsTags, cancellationToken);
    }

    public Task RemoveRangeAsync(IEnumerable<NewsTag> newsTags, CancellationToken cancellationToken = default)
    {
        return _newsTagDao.RemoveRangeAsync(newsTags, cancellationToken);
    }

    public Task ReplaceTagsForArticleAsync(string articleId, IEnumerable<int> tagIds, CancellationToken cancellationToken = default)
    {
        return _newsTagDao.ReplaceTagsForArticleAsync(articleId, tagIds, cancellationToken);
    }

    public Task DeleteByArticleIdAsync(string articleId, CancellationToken cancellationToken = default)
    {
        return _newsTagDao.DeleteByArticleIdAsync(articleId, cancellationToken);
    }
}
