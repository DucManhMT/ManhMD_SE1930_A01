using System.Threading;
using System.Threading.Tasks;
using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.BusinessLogic.Services;

public interface IAuditLogClientService
{
    Task<AuditLogPagedResponseApiModel> GetAuditLogsAsync(
        int? userId = null,
        string? entityType = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
