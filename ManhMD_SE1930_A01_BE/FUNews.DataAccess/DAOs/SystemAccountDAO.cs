using FUNews.DataAccess.Context;
using FUNews.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace FUNews.DataAccess.DAOs;

public class SystemAccountDAO
{
    private readonly FUNewsDbContext _context;

    public SystemAccountDAO(FUNewsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<SystemAccount?> GetByIdAsync(short id, CancellationToken cancellationToken = default)
    {
        return await _context.SystemAccounts
            .FirstOrDefaultAsync(a => a.AccountID == id, cancellationToken);
    }

    public async Task<SystemAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var trimmedEmail = email.Trim().ToLower();

        return await _context.SystemAccounts
            .FirstOrDefaultAsync(a => a.AccountEmail != null && a.AccountEmail.Trim().ToLower() == trimmedEmail, cancellationToken);
    }

    public async Task<List<SystemAccount>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SystemAccounts
            .AsNoTracking()
            .OrderBy(a => a.AccountID)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(short id, CancellationToken cancellationToken = default)
    {
        return await _context.SystemAccounts
            .AnyAsync(a => a.AccountID == id, cancellationToken);
    }

    public async Task<bool> IsEmailUniqueAsync(string email, short? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return true;
        var trimmedEmail = email.Trim().ToLower();

        var query = _context.SystemAccounts
            .Where(a => a.AccountEmail != null && a.AccountEmail.Trim().ToLower() == trimmedEmail);

        if (excludeId.HasValue)
        {
            query = query.Where(a => a.AccountID != excludeId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> HasCreatedArticlesAsync(short id, CancellationToken cancellationToken = default)
    {
        return await _context.NewsArticles
            .AnyAsync(a => a.CreatedByID == id, cancellationToken);
    }

    public async Task<bool> HasUpdatedArticlesAsync(short id, CancellationToken cancellationToken = default)
    {
        return await _context.NewsArticles
            .AnyAsync(a => a.UpdatedByID == id, cancellationToken);
    }

    public async Task<SystemAccount> AddAsync(SystemAccount account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        await _context.SystemAccounts.AddAsync(account, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<SystemAccount> UpdateAsync(SystemAccount account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        _context.SystemAccounts.Update(account);
        await _context.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<bool> DeleteAsync(short id, CancellationToken cancellationToken = default)
    {
        var account = await _context.SystemAccounts.FindAsync(new object[] { id }, cancellationToken);
        if (account == null)
        {
            return false;
        }

        if (await HasCreatedArticlesAsync(id, cancellationToken))
        {
            throw new InvalidOperationException($"Cannot delete SystemAccount with ID {id} because it is referenced as creator of news articles.");
        }

        if (await HasUpdatedArticlesAsync(id, cancellationToken))
        {
            throw new InvalidOperationException($"Cannot delete SystemAccount with ID {id} because it is referenced as editor of news articles.");
        }

        _context.SystemAccounts.Remove(account);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
