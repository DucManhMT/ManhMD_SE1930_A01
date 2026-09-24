using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Exceptions;
using FUNews.BusinessLogic.Helpers;
using FUNews.BusinessLogic.Models;
using FUNews.DataAccess.Entities;
using FUNews.DataAccess.Repositories;
using FUNews.DataAccess.Security;
using FUNews.DataAccess.Sequences;
using Microsoft.EntityFrameworkCore;

namespace FUNews.BusinessLogic.Services;

public class AccountService : IAccountService
{
    private readonly ISystemAccountRepository _accountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISqlSequenceService _sqlSequenceService;

    public AccountService(
        ISystemAccountRepository accountRepository,
        IPasswordHasher passwordHasher,
        ISqlSequenceService sqlSequenceService)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _sqlSequenceService = sqlSequenceService ?? throw new ArgumentNullException(nameof(sqlSequenceService));
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
        return account != null ? AccountMappingHelper.ToDto(account) : null;
    }

    public async Task<AccountDto> CreateAsync(CreateAccountRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var trimmedName = AccountValidationHelper.ValidateAndTrimName(request.AccountName);
        var trimmedEmail = AccountValidationHelper.ValidateAndTrimEmail(request.AccountEmail);
        var role = AccountValidationHelper.ValidateRole(request.AccountRole);
        AccountValidationHelper.ValidatePassword(request.AccountPassword);

        var isUnique = await _accountRepository.IsEmailUniqueAsync(trimmedEmail, null, cancellationToken);
        if (!isUnique)
        {
            throw new ValidationException(nameof(request.AccountEmail), "Email này đã được sử dụng bởi một tài khoản khác trong hệ thống.");
        }

        var hashedPassword = _passwordHasher.HashPassword(request.AccountPassword);

        short nextId = await _sqlSequenceService.GetNextAccountIdAsync(cancellationToken);
        while (await _accountRepository.ExistsAsync(nextId, cancellationToken))
        {
            nextId++;
        }

        var account = new SystemAccount
        {
            AccountID = nextId,
            AccountName = trimmedName,
            AccountEmail = trimmedEmail,
            AccountRole = role,
            AccountPassword = hashedPassword
        };

        try
        {
            var created = await _accountRepository.AddAsync(account, cancellationToken);
            return AccountMappingHelper.ToDto(created);
        }
        catch (DbUpdateException ex)
        {
            AccountValidationHelper.HandleDbUpdateException(ex);
            throw;
        }
    }

    public async Task<AccountDto> UpdateAsync(short id, UpdateAccountRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var trimmedName = AccountValidationHelper.ValidateAndTrimName(request.AccountName);
        var trimmedEmail = AccountValidationHelper.ValidateAndTrimEmail(request.AccountEmail);
        var role = AccountValidationHelper.ValidateRole(request.AccountRole);

        var account = await _accountRepository.GetByIdAsync(id, cancellationToken);
        if (account == null)
        {
            throw new NotFoundException($"Tài khoản với mã {id} không tồn tại.");
        }

        var isUnique = await _accountRepository.IsEmailUniqueAsync(trimmedEmail, id, cancellationToken);
        if (!isUnique)
        {
            throw new ValidationException(nameof(request.AccountEmail), "Email này đã được sử dụng bởi một tài khoản khác trong hệ thống.");
        }

        account.AccountName = trimmedName;
        account.AccountEmail = trimmedEmail;
        account.AccountRole = role;

        try
        {
            var updated = await _accountRepository.UpdateAsync(account, cancellationToken);
            return AccountMappingHelper.ToDto(updated);
        }
        catch (DbUpdateException ex)
        {
            AccountValidationHelper.HandleDbUpdateException(ex);
            throw;
        }
    }

    public async Task DeleteAsync(short id, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(id, cancellationToken);
        if (account == null)
        {
            throw new NotFoundException($"Tài khoản với mã {id} không tồn tại.");
        }

        if (await _accountRepository.HasCreatedArticlesAsync(id, cancellationToken))
        {
            throw new ConflictException("Không thể xóa tài khoản vì tài khoản này đã được sử dụng làm tác giả (CreatedBy) của bài viết.");
        }

        if (await _accountRepository.HasUpdatedArticlesAsync(id, cancellationToken))
        {
            throw new ConflictException("Không thể xóa tài khoản vì tài khoản này đã được sử dụng làm người chỉnh sửa (UpdatedBy) của bài viết.");
        }

        var deleted = await _accountRepository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            throw new NotFoundException($"Tài khoản với mã {id} không tồn tại.");
        }
    }
}
