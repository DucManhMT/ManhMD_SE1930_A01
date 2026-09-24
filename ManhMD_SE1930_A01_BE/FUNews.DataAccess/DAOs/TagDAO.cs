using FUNews.DataAccess.Context;
using FUNews.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace FUNews.DataAccess.DAOs;

public class TagDAO
{
    private readonly FUNewsDbContext _context;

    public TagDAO(FUNewsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IQueryable<Tag> GetQueryable()
    {
        return _context.Tags.OrderBy(t => t.TagID).AsQueryable();
    }

    public async Task<Tag?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Tags
            .FirstOrDefaultAsync(t => t.TagID == id, cancellationToken);
    }

    public async Task<List<Tag>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        return await _context.Tags
            .Where(t => idList.Contains(t.TagID))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Tag>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tags
            .AsNoTracking()
            .OrderBy(t => t.TagID)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Tags
            .AnyAsync(t => t.TagID == id, cancellationToken);
    }

    public async Task<bool> IsNameUniqueAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return true;
        var trimmedName = name.Trim().ToLower();

        var query = _context.Tags
            .Where(t => t.TagName != null && t.TagName.Trim().ToLower() == trimmedName);

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.TagID != excludeId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> HasArticlesAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.NewsTags
            .AnyAsync(nt => nt.TagID == id, cancellationToken);
    }

    public async Task<Tag?> GetByIdWithNewsTagsAsync(int id, bool asNoTracking = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Tags
            .Include(t => t.NewsTags)
                .ThenInclude(nt => nt.NewsArticle);

        if (asNoTracking)
        {
            return await query.AsNoTracking().FirstOrDefaultAsync(t => t.TagID == id, cancellationToken);
        }

        return await query.FirstOrDefaultAsync(t => t.TagID == id, cancellationToken);
    }

    public async Task<List<NewsArticle>> GetArticlesByTagAsync(int tagId, bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var query = _context.NewsArticles
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.CreatedBy)
            .Include(a => a.UpdatedBy)
            .Include(a => a.NewsTags)
                .ThenInclude(nt => nt.Tag)
            .Where(a => a.NewsTags.Any(nt => nt.TagID == tagId));

        if (activeOnly == true)
        {
            query = query.Where(a => a.NewsStatus == true);
        }

        return await query
            .OrderByDescending(a => a.CreatedDate)
            .ThenByDescending(a => a.NewsArticleID)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tag> AddAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tag);
        await _context.Tags.AddAsync(tag, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return tag;
    }

    public async Task<Tag> UpdateAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tag);
        _context.Tags.Update(tag);
        await _context.SaveChangesAsync(cancellationToken);
        return tag;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var tag = await _context.Tags.FindAsync(new object[] { id }, cancellationToken);
        if (tag == null)
        {
            return false;
        }

        if (await HasArticlesAsync(id, cancellationToken))
        {
            throw new InvalidOperationException($"Cannot delete Tag with ID {id} because it is associated with existing news articles.");
        }

        _context.Tags.Remove(tag);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
