using FUNews.DataAccess.Context;
using FUNews.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace FUNews.DataAccess.DAOs;

public class NewsTagDAO
{
    private readonly FUNewsDbContext _context;

    public NewsTagDAO(FUNewsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<NewsTag>> GetByArticleIdAsync(string articleId, CancellationToken cancellationToken = default)
    {
        return await _context.NewsTags
            .Include(nt => nt.Tag)
            .Where(nt => nt.NewsArticleID == articleId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<NewsTag>> GetByTagIdAsync(int tagId, CancellationToken cancellationToken = default)
    {
        return await _context.NewsTags
            .Include(nt => nt.NewsArticle)
            .Where(nt => nt.TagID == tagId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<NewsTag> newsTags, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(newsTags);
        await _context.NewsTags.AddRangeAsync(newsTags, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveRangeAsync(IEnumerable<NewsTag> newsTags, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(newsTags);
        _context.NewsTags.RemoveRange(newsTags);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceTagsForArticleAsync(string articleId, IEnumerable<int> tagIds, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(articleId))
        {
            throw new ArgumentException("Article ID cannot be null or empty.", nameof(articleId));
        }

        var existingTags = await _context.NewsTags
            .Where(nt => nt.NewsArticleID == articleId)
            .ToListAsync(cancellationToken);

        _context.NewsTags.RemoveRange(existingTags);

        var distinctTagIds = tagIds?.Distinct().ToList() ?? new List<int>();
        var newNewsTags = distinctTagIds.Select(tid => new NewsTag
        {
            NewsArticleID = articleId,
            TagID = tid
        }).ToList();

        if (newNewsTags.Count > 0)
        {
            await _context.NewsTags.AddRangeAsync(newNewsTags, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByArticleIdAsync(string articleId, CancellationToken cancellationToken = default)
    {
        var existingTags = await _context.NewsTags
            .Where(nt => nt.NewsArticleID == articleId)
            .ToListAsync(cancellationToken);

        if (existingTags.Count > 0)
        {
            _context.NewsTags.RemoveRange(existingTags);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
