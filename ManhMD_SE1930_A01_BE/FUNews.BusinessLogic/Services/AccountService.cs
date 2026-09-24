using FUNews.BusinessLogic.DTOs;
using FUNews.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FUNews.BusinessLogic.Services;

public class AccountService : IAccountService
{
    private readonly ISystemAccountRepository _accountRepository;

    public AccountService(ISystemAccountRepository accountRepository)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
    }

    public IQueryable<AccountDto> GetQueryable()
    {
        return _accountRepository.GetQueryable()
            .AsNoTracking()
            .Select(a => new AccountDto
            {
                AccountId = a.AccountID,
                AccountName = a.AccountName,
                AccountEmail = a.AccountEmail,
                AccountRole = a.AccountRole,
                RoleName = a.AccountRole == 1 ? "Staff" : a.AccountRole == 2 ? "Lecturer" : "Unknown"
            });
    }

    public async Task<AccountDto?> GetByIdAsync(short id, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(id, cancellationToken);
        if (account == null) return null;

        return new AccountDto
        {
            AccountId = account.AccountID,
            AccountName = account.AccountName,
            AccountEmail = account.AccountEmail,
            AccountRole = account.AccountRole,
            RoleName = account.AccountRole == 1 ? "Staff" : account.AccountRole == 2 ? "Lecturer" : "Unknown"
        };
    }
}
