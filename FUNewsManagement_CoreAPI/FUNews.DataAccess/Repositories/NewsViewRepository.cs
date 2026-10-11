using FUNews.DataAccess.Context;
using FUNews.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace FUNews.DataAccess.Repositories;

public class NewsViewRepository : INewsViewRepository
{
    private readonly FUNewsDbContext _context;

    public NewsViewRepository(FUNewsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task RecordViewAsync(string newsArticleId, string? visitorIp = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newsArticleId);

        var view = new NewsView
        {
            NewsArticleID = newsArticleId,
            ViewedAt = DateTime.UtcNow,
            VisitorIP = visitorIp
        };

        _context.NewsViews.Add(view);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetViewStatsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.NewsViews
            .AsNoTracking()
            .GroupBy(v => v.NewsArticleID)
            .Select(g => new { ArticleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ArticleId, x => x.Count, cancellationToken);
    }

    public async Task<int> GetViewCountAsync(string newsArticleId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newsArticleId);

        return await _context.NewsViews
            .AsNoTracking()
            .CountAsync(v => v.NewsArticleID == newsArticleId, cancellationToken);
    }
}
