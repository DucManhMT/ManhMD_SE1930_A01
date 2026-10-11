using FUNews.DataAccess.Context;
using FUNews.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace FUNews.DataAccess.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly FUNewsDbContext _context;

    public AuditLogRepository(FUNewsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task AddAsync(AuditLog log, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(log);
        await _context.AuditLogs.AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(List<AuditLog> Items, int TotalCount)> GetLogsAsync(
        int? userId = null,
        string? entityType = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(a => a.UserID == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.Entity.ToLower() == entityType.Trim().ToLower());
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize < 1 ? 20 : (pageSize > 100 ? 100 : pageSize);

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public IQueryable<AuditLog> GetQueryable()
    {
        return _context.AuditLogs.AsNoTracking();
    }
}
