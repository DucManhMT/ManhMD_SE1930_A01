using FUNews.BusinessLogic.DTOs;

namespace FUNews.BusinessLogic.Services;

public interface IAuditService
{
    Task<AuditLogPagedResponseDto> GetAuditLogsAsync(
        int? userId = null,
        string? entityType = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
