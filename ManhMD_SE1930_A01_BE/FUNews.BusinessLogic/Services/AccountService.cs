using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Exceptions;
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

    public async Task<AccountDto> CreateAsync(CreateAccountRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Validate AccountName
        var trimmedName = request.AccountName?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw new ValidationException(nameof(request.AccountName), "Họ và tên là bắt buộc.");
        }
        if (trimmedName.Length > 100)
        {
            throw new ValidationException(nameof(request.AccountName), "Họ và tên không được vượt quá 100 ký tự.");
        }

        // 2. Validate AccountEmail
        var trimmedEmail = request.AccountEmail?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedEmail))
        {
            throw new ValidationException(nameof(request.AccountEmail), "Email là bắt buộc.");
        }
        if (trimmedEmail.Length > 70)
        {
            throw new ValidationException(nameof(request.AccountEmail), "Email không được vượt quá 70 ký tự.");
        }

        // 3. Validate AccountRole (Acceptance criteria 5: Role ngoài 1/2 bị từ chối)
        if (!request.AccountRole.HasValue || (request.AccountRole.Value != 1 && request.AccountRole.Value != 2))
        {
            throw new ValidationException(nameof(request.AccountRole), "Vai trò không hợp lệ. Chỉ chấp nhận Nhân viên (1) hoặc Giảng viên (2).");
        }

        // 4. Validate AccountPassword
        if (string.IsNullOrWhiteSpace(request.AccountPassword) || request.AccountPassword.Length < 6)
        {
            throw new ValidationException(nameof(request.AccountPassword), "Mật khẩu phải có độ dài tối thiểu 6 ký tự.");
        }

        // 5. Email uniqueness check (Acceptance criteria 1: Email trùng bị chặn client/server)
        var isUnique = await _accountRepository.IsEmailUniqueAsync(trimmedEmail, null, cancellationToken);
        if (!isUnique)
        {
            throw new ValidationException(nameof(request.AccountEmail), "Email này đã được sử dụng bởi một tài khoản khác trong hệ thống.");
        }

        // 6. Hash password with BCrypt (Acceptance criteria 2: Hash lưu DB)
        var hashedPassword = _passwordHasher.HashPassword(request.AccountPassword);

        // 7. Generate safe sequential AccountId
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
            AccountRole = request.AccountRole.Value,
            AccountPassword = hashedPassword
        };

        try
        {
            var created = await _accountRepository.AddAsync(account, cancellationToken);

            return new AccountDto
            {
                AccountId = created.AccountID,
                AccountName = created.AccountName,
                AccountEmail = created.AccountEmail,
                AccountRole = created.AccountRole,
                RoleName = created.AccountRole == 1 ? "Staff" : created.AccountRole == 2 ? "Lecturer" : "Unknown"
            };
        }
        catch (DbUpdateException)
        {
            throw new ValidationException(nameof(request.AccountEmail), "Email này đã được sử dụng bởi một tài khoản khác trong hệ thống.");
        }
    }
}

