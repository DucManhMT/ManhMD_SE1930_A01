using FUNews.DataAccess.Entities;

namespace FUNews.DataAccess.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task RevokeUserTokensAsync(short? accountId, string? userEmail, CancellationToken cancellationToken = default);
}
