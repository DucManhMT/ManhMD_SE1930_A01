using FUNews.DataAccess.Context;
using FUNews.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FUNews.DataAccess.DAOs;

public class NewsArticleDAO
{
    private readonly FUNewsDbContext _context;

    public NewsArticleDAO(FUNewsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IQueryable<NewsArticle> GetQueryable()
    {
        return _context.NewsArticles.AsQueryable();
    }

    public async Task<NewsArticle?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.NewsArticles
            .FirstOrDefaultAsync(a => a.NewsArticleID == id, cancellationToken);
    }

    public async Task<NewsArticle?> GetByIdWithDetailsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.NewsArticles
            .Include(a => a.Category)
            .Include(a => a.CreatedBy)
            .Include(a => a.UpdatedBy)
            .Include(a => a.NewsTags)
                .ThenInclude(nt => nt.Tag)
            .FirstOrDefaultAsync(a => a.NewsArticleID == id, cancellationToken);
    }

    public async Task<List<NewsArticle>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NewsArticles
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedDate)
            .ThenByDescending(a => a.NewsArticleID)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.NewsArticles
            .AnyAsync(a => a.NewsArticleID == id, cancellationToken);
    }

    public async Task<NewsArticle> AddAsync(NewsArticle article, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(article);
        await _context.NewsArticles.AddAsync(article, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return article;
    }

    public async Task<NewsArticle> CreateWithTagsAsync(NewsArticle article, IEnumerable<int> tagIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(article);

        IDbContextTransaction? transaction = null;
        if (_context.Database.IsRelational())
        {
            transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            await _context.NewsArticles.AddAsync(article, cancellationToken);

            var distinctTagIds = tagIds?.Distinct().ToList() ?? new List<int>();
            foreach (var tagId in distinctTagIds)
            {
                await _context.NewsTags.AddAsync(new NewsTag
                {
                    NewsArticleID = article.NewsArticleID,
                    TagID = tagId
                }, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return article;
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task<NewsArticle> UpdateAsync(NewsArticle article, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(article);
        _context.NewsArticles.Update(article);
        await _context.SaveChangesAsync(cancellationToken);
        return article;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var article = await _context.NewsArticles.FindAsync(new object[] { id }, cancellationToken);
        if (article == null)
        {
            return false;
        }

        // Delete associated NewsTags first to enforce explicit deletion as specified in decisions
        var tags = await _context.NewsTags.Where(nt => nt.NewsArticleID == id).ToListAsync(cancellationToken);
        if (tags.Count > 0)
        {
            _context.NewsTags.RemoveRange(tags);
        }

        _context.NewsArticles.Remove(article);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
