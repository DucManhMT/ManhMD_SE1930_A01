using FUNews.DataAccess.Context;
using FUNews.DataAccess.Security;
using Microsoft.EntityFrameworkCore;

namespace FUNews.DataAccess.Seeding;

public class PasswordSeeder : IPasswordSeeder
{
    private readonly FUNewsDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public PasswordSeeder(FUNewsDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
    }

    public async Task<int> SeedLegacyPasswordsAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _context.SystemAccounts
            .Where(a => a.AccountPassword != null && a.AccountPassword != "")
            .ToListAsync(cancellationToken);

        int updatedCount = 0;
        foreach (var account in accounts)
        {
            if (!_passwordHasher.IsHashed(account.AccountPassword))
            {
                account.AccountPassword = _passwordHasher.HashPassword(account.AccountPassword!);
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return updatedCount;
    }
}
