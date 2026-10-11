using System;
using System.Threading;
using System.Threading.Tasks;
using FUNews.Client.DataAccess.Clients;
using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.BusinessLogic.Services;

public class AuditLogClientService : IAuditLogClientService
{
    private readonly ICoreApiClient _coreApiClient;

    public AuditLogClientService(ICoreApiClient coreApiClient)
    {
        _coreApiClient = coreApiClient ?? throw new ArgumentNullException(nameof(coreApiClient));
    }

    public Task<AuditLogPagedResponseApiModel> GetAuditLogsAsync(
        int? userId = null,
        string? entityType = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return _coreApiClient.GetAuditLogsAsync(userId, entityType, page, pageSize, cancellationToken);
    }
}
