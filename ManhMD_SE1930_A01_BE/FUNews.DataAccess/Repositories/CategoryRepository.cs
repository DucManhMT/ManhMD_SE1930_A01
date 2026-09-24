using FUNews.DataAccess.DAOs;
using FUNews.DataAccess.Entities;

namespace FUNews.DataAccess.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly CategoryDAO _categoryDao;

    public CategoryRepository(CategoryDAO categoryDao)
    {
        _categoryDao = categoryDao ?? throw new ArgumentNullException(nameof(categoryDao));
    }

    public Task<Category?> GetByIdAsync(short id, CancellationToken cancellationToken = default)
    {
        return _categoryDao.GetByIdAsync(id, cancellationToken);
    }

    public Task<Category?> GetByIdWithParentAndChildrenAsync(short id, CancellationToken cancellationToken = default)
    {
        return _categoryDao.GetByIdWithParentAndChildrenAsync(id, cancellationToken);
    }

    public Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _categoryDao.GetAllAsync(cancellationToken);
    }

    public Task<List<Category>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return _categoryDao.GetActiveAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(short id, CancellationToken cancellationToken = default)
    {
        return _categoryDao.ExistsAsync(id, cancellationToken);
    }

    public Task<bool> HasChildrenAsync(short id, CancellationToken cancellationToken = default)
    {
        return _categoryDao.HasChildrenAsync(id, cancellationToken);
    }

    public Task<bool> HasArticlesAsync(short id, CancellationToken cancellationToken = default)
    {
        return _categoryDao.HasArticlesAsync(id, cancellationToken);
    }

    public Task<bool> IsNameUniqueAsync(string name, short? parentId, short? excludeId = null, CancellationToken cancellationToken = default)
    {
        return _categoryDao.IsNameUniqueAsync(name, parentId, excludeId, cancellationToken);
    }

    public Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        return _categoryDao.AddAsync(category, cancellationToken);
    }

    public Task<Category> UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        return _categoryDao.UpdateAsync(category, cancellationToken);
    }

    public Task<bool> DeleteAsync(short id, CancellationToken cancellationToken = default)
    {
        return _categoryDao.DeleteAsync(id, cancellationToken);
    }
}
