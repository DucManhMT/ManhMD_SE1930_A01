using FUNews.DataAccess.Entities;

namespace FUNews.DataAccess.Repositories;

public interface ICategoryRepository
{
    IQueryable<Category> GetQueryable();
    Task<Category?> GetByIdAsync(short id, CancellationToken cancellationToken = default);
    Task<Category?> GetByIdWithParentAndChildrenAsync(short id, CancellationToken cancellationToken = default);
    Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Category>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(short id, CancellationToken cancellationToken = default);
    Task<bool> HasChildrenAsync(short id, CancellationToken cancellationToken = default);
    Task<bool> HasArticlesAsync(short id, CancellationToken cancellationToken = default);
    Task<bool> IsNameUniqueAsync(string name, short? parentId, short? excludeId = null, CancellationToken cancellationToken = default);
    Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default);
    Task<Category> UpdateAsync(Category category, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(short id, CancellationToken cancellationToken = default);
}
