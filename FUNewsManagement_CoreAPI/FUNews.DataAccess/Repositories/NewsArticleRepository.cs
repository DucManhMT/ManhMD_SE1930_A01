using FUNews.DataAccess.DAOs;
using FUNews.DataAccess.Entities;

namespace FUNews.DataAccess.Repositories;

public class NewsArticleRepository : INewsArticleRepository
{
    private readonly NewsArticleDAO _articleDao;

    public NewsArticleRepository(NewsArticleDAO articleDao)
    {
        _articleDao = articleDao ?? throw new ArgumentNullException(nameof(articleDao));
    }

    public IQueryable<NewsArticle> GetQueryable()
    {
        return _articleDao.GetQueryable();
    }

    public Task<NewsArticle?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return _articleDao.GetByIdAsync(id, cancellationToken);
    }

    public Task<NewsArticle?> GetByIdWithDetailsAsync(string id, CancellationToken cancellationToken = default)
    {
        return _articleDao.GetByIdWithDetailsAsync(id, cancellationToken);
    }

    public Task<List<NewsArticle>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _articleDao.GetAllAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return _articleDao.ExistsAsync(id, cancellationToken);
    }

    public Task<NewsArticle> AddAsync(NewsArticle article, CancellationToken cancellationToken = default)
    {
        return _articleDao.AddAsync(article, cancellationToken);
    }

    public Task<NewsArticle> CreateWithTagsAsync(NewsArticle article, IEnumerable<int> tagIds, CancellationToken cancellationToken = default)
    {
        return _articleDao.CreateWithTagsAsync(article, tagIds, cancellationToken);
    }

    public Task<NewsArticle> UpdateAsync(NewsArticle article, CancellationToken cancellationToken = default)
    {
        return _articleDao.UpdateAsync(article, cancellationToken);
    }

    public Task<NewsArticle> UpdateWithTagsAsync(NewsArticle article, IEnumerable<int> tagIds, CancellationToken cancellationToken = default)
    {
        return _articleDao.UpdateWithTagsAsync(article, tagIds, cancellationToken);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return _articleDao.DeleteAsync(id, cancellationToken);
    }
}
