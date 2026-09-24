using FUNews.DataAccess.Entities;

namespace FUNews.DataAccess.Repositories;

public interface INewsArticleRepository
{
    IQueryable<NewsArticle> GetQueryable();
    Task<NewsArticle?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<NewsArticle?> GetByIdWithDetailsAsync(string id, CancellationToken cancellationToken = default);
    Task<List<NewsArticle>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
    Task<NewsArticle> AddAsync(NewsArticle article, CancellationToken cancellationToken = default);
    Task<NewsArticle> CreateWithTagsAsync(NewsArticle article, IEnumerable<int> tagIds, CancellationToken cancellationToken = default);
    Task<NewsArticle> UpdateAsync(NewsArticle article, CancellationToken cancellationToken = default);
    Task<NewsArticle> UpdateWithTagsAsync(NewsArticle article, IEnumerable<int> tagIds, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}

