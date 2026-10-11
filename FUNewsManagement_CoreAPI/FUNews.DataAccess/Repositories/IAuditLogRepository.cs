using FUNews.DataAccess.Entities;

namespace FUNews.DataAccess.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken cancellationToken = default);
    Task<(List<AuditLog> Items, int TotalCount)> GetLogsAsync(int? userId = null, string? entityType = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    IQueryable<AuditLog> GetQueryable();
}
