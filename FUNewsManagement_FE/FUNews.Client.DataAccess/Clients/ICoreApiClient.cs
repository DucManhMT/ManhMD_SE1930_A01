using FUNews.Client.DataAccess.Models;

namespace FUNews.Client.DataAccess.Clients;

public interface ICoreApiClient : IFUNewsApiClient
{
    Task<LoginResponseApiModel> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<AuditLogPagedResponseApiModel> GetAuditLogsAsync(int? userId = null, string? entityType = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}
