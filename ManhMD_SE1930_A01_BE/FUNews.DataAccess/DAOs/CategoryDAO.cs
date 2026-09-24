using FUNews.DataAccess.Context;
using FUNews.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace FUNews.DataAccess.DAOs;

public class CategoryDAO
{
    private readonly FUNewsDbContext _context;

    public CategoryDAO(FUNewsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Category?> GetByIdAsync(short id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryID == id, cancellationToken);
    }

    public async Task<Category?> GetByIdWithParentAndChildrenAsync(short id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Include(c => c.ParentCategory)
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.CategoryID == id, cancellationToken);
    }

    public async Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.CategoryID)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Category>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive == true)
            .OrderBy(c => c.CategoryName)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(short id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AnyAsync(c => c.CategoryID == id, cancellationToken);
    }

    public async Task<bool> HasChildrenAsync(short id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AnyAsync(c => c.ParentCategoryID == id, cancellationToken);
    }

    public async Task<bool> HasArticlesAsync(short id, CancellationToken cancellationToken = default)
    {
        return await _context.NewsArticles
            .AnyAsync(a => a.CategoryID == id, cancellationToken);
    }

    public async Task<bool> IsNameUniqueAsync(string name, short? parentId, short? excludeId = null, CancellationToken cancellationToken = default)
    {
        var trimmedName = name.Trim();
        var query = _context.Categories.Where(c => c.CategoryName.Trim().ToLower() == trimmedName.ToLower());

        if (parentId.HasValue)
        {
            query = query.Where(c => c.ParentCategoryID == parentId.Value);
        }
        else
        {
            query = query.Where(c => c.ParentCategoryID == null);
        }

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.CategoryID != excludeId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(category);
        await _context.Categories.AddAsync(category, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task<Category> UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(category);
        _context.Categories.Update(category);
        await _context.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task<bool> DeleteAsync(short id, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories.FindAsync(new object[] { id }, cancellationToken);
        if (category == null)
        {
            return false;
        }

        if (await HasArticlesAsync(id, cancellationToken))
        {
            throw new InvalidOperationException($"Cannot delete Category with ID {id} because it is referenced by existing news articles.");
        }

        if (await HasChildrenAsync(id, cancellationToken))
        {
            throw new InvalidOperationException($"Cannot delete Category with ID {id} because it has child sub-categories.");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
