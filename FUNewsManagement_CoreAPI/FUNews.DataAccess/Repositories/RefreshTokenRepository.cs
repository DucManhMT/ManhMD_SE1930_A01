using FUNews.DataAccess.Context;
using FUNews.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace FUNews.DataAccess.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly FUNewsDbContext _context;

    public RefreshTokenRepository(FUNewsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Include(r => r.Account)
            .FirstOrDefaultAsync(r => r.Token == token, cancellationToken);
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(refreshToken);
        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(refreshToken);
        _context.RefreshTokens.Update(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeUserTokensAsync(short? accountId, string? userEmail, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(r => !r.IsRevoked && ((accountId.HasValue && r.AccountID == accountId) || (!string.IsNullOrEmpty(userEmail) && r.UserEmail == userEmail)))
            .ToListAsync(cancellationToken);

        foreach (var t in tokens)
        {
            t.IsRevoked = true;
        }

        if (tokens.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
