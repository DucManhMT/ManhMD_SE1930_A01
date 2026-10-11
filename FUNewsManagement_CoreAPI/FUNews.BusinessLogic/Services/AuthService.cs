using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Exceptions;
using FUNews.BusinessLogic.Helpers;
using FUNews.BusinessLogic.Models;
using FUNews.BusinessLogic.Options;
using FUNews.BusinessLogic.Security;
using FUNews.DataAccess.Repositories;
using FUNews.DataAccess.Security;

namespace FUNews.BusinessLogic.Services;

public class AuthService : IAuthService
{
    private readonly ISystemAccountRepository _accountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly DefaultAdminOptions _adminOptions;

    public AuthService(
        ISystemAccountRepository accountRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        DefaultAdminOptions adminOptions)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _adminOptions = adminOptions ?? throw new ArgumentNullException(nameof(adminOptions));
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (trimmedEmail, plainPassword) = AuthHelper.ValidateAndExtractCredentials(request.Email, request.Password);

        // 1. Kiểm tra tài khoản Quản trị viên (Admin) từ file cấu hình appsettings.json
        if (!string.IsNullOrWhiteSpace(_adminOptions.Email) &&
            string.Equals(trimmedEmail, _adminOptions.Email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(plainPassword, _adminOptions.Password))
            {
                var adminUser = AuthHelper.CreateAdminUserInfo(_adminOptions.Email);
                var token = _jwtTokenService.GenerateToken(adminUser, out var expiresAt);
                return new LoginResponseDto
                {
                    Token = token,
                    ExpiresAt = expiresAt,
                    User = adminUser
                };
            }

            throw new UnauthorizedException("Email hoặc mật khẩu không chính xác.");
        }

        // 2. Kiểm tra tài khoản Staff / Lecturer trong CSDL SystemAccount
        var account = await _accountRepository.GetByEmailAsync(trimmedEmail, cancellationToken);
        if (account == null)
        {
            throw new UnauthorizedException("Email hoặc mật khẩu không chính xác.");
        }

        // Xác thực mật khẩu thông qua IPasswordHasher (BCrypt)
        if (string.IsNullOrEmpty(account.AccountPassword) ||
            !_passwordHasher.VerifyPassword(plainPassword, account.AccountPassword))
        {
            throw new UnauthorizedException("Email hoặc mật khẩu không chính xác.");
        }

        // Phân quyền theo AccountRole và tạo thông tin UserInfo an toàn
        var userInfo = AuthHelper.CreateAccountUserInfo(account);
        var jwtToken = _jwtTokenService.GenerateToken(userInfo, out var tokenExpiresAt);

        return new LoginResponseDto
        {
            Token = jwtToken,
            ExpiresAt = tokenExpiresAt,
            User = userInfo
        };
    }
}
