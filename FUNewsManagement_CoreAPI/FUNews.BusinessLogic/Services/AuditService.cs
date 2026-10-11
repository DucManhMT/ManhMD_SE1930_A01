using FUNews.BusinessLogic.DTOs;
using FUNews.DataAccess.Repositories;

namespace FUNews.BusinessLogic.Services;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
    }

    public async Task<AuditLogPagedResponseDto> GetAuditLogsAsync(
        int? userId = null,
        string? entityType = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _auditLogRepository.GetLogsAsync(userId, entityType, page, pageSize, cancellationToken);

        return new AuditLogPagedResponseDto
        {
            Items = items.Select(log => new AuditLogDto
            {
                AuditId = log.AuditID,
                UserId = log.UserID,
                UserName = log.UserName,
                Action = log.Action,
                Entity = log.Entity,
                EntityId = log.EntityID,
                BeforeJson = log.BeforeJSON,
                AfterJson = log.AfterJSON,
                Timestamp = log.Timestamp
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
