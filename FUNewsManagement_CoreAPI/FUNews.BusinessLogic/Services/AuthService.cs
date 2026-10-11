using System.Security.Cryptography;
using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Exceptions;
using FUNews.BusinessLogic.Helpers;
using FUNews.BusinessLogic.Models;
using FUNews.BusinessLogic.Options;
using FUNews.BusinessLogic.Security;
using FUNews.DataAccess.Entities;
using FUNews.DataAccess.Repositories;
using FUNews.DataAccess.Security;

namespace FUNews.BusinessLogic.Services;

public class AuthService : IAuthService
{
    private readonly ISystemAccountRepository _accountRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly DefaultAdminOptions _adminOptions;

    public AuthService(
        ISystemAccountRepository accountRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        DefaultAdminOptions adminOptions)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
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
                var refreshTokenString = GenerateRefreshTokenString();
                var refreshExpiresAt = DateTime.UtcNow.AddDays(7);

                var refreshTokenEntity = new RefreshToken
                {
                    UserEmail = _adminOptions.Email,
                    Token = refreshTokenString,
                    ExpiresAt = refreshExpiresAt,
                    CreatedAt = DateTime.UtcNow,
                    IsRevoked = false
                };
                await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

                return new LoginResponseDto
                {
                    Token = token,
                    AccessToken = token,
                    RefreshToken = refreshTokenString,
                    ExpiresAt = expiresAt,
                    RefreshExpiresAt = refreshExpiresAt,
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
        var dbRefreshTokenString = GenerateRefreshTokenString();
        var dbRefreshExpiresAt = DateTime.UtcNow.AddDays(7);

        var dbRefreshTokenEntity = new RefreshToken
        {
            AccountID = account.AccountID,
            UserEmail = account.AccountEmail,
            Token = dbRefreshTokenString,
            ExpiresAt = dbRefreshExpiresAt,
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };
        await _refreshTokenRepository.AddAsync(dbRefreshTokenEntity, cancellationToken);

        return new LoginResponseDto
        {
            Token = jwtToken,
            AccessToken = jwtToken,
            RefreshToken = dbRefreshTokenString,
            ExpiresAt = tokenExpiresAt,
            RefreshExpiresAt = dbRefreshExpiresAt,
            User = userInfo
        };
    }

    public async Task<LoginResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedException("Refresh token không được để trống.");
        }

        var existingToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken, cancellationToken);
        if (existingToken == null || existingToken.IsRevoked || existingToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Refresh token không hợp lệ hoặc đã hết hạn.");
        }

        // Token Rotation: Thu hồi token cũ
        var newRefreshTokenString = GenerateRefreshTokenString();
        var newRefreshExpiresAt = DateTime.UtcNow.AddDays(7);

        existingToken.IsRevoked = true;
        existingToken.ReplacedByToken = newRefreshTokenString;
        await _refreshTokenRepository.UpdateAsync(existingToken, cancellationToken);

        UserInfoDto userInfo;
        if (!string.IsNullOrWhiteSpace(existingToken.UserEmail) &&
            string.Equals(existingToken.UserEmail, _adminOptions.Email, StringComparison.OrdinalIgnoreCase))
        {
            userInfo = AuthHelper.CreateAdminUserInfo(_adminOptions.Email);
        }
        else if (existingToken.AccountID.HasValue)
        {
            var account = await _accountRepository.GetByIdAsync(existingToken.AccountID.Value, cancellationToken);
            if (account == null)
            {
                throw new UnauthorizedException("Tài khoản người dùng không tồn tại.");
            }
            userInfo = AuthHelper.CreateAccountUserInfo(account);
        }
        else
        {
            throw new UnauthorizedException("Thông tin người dùng không hợp lệ.");
        }

        var newJwtToken = _jwtTokenService.GenerateToken(userInfo, out var tokenExpiresAt);

        var newTokenEntity = new RefreshToken
        {
            AccountID = existingToken.AccountID,
            UserEmail = existingToken.UserEmail,
            Token = newRefreshTokenString,
            ExpiresAt = newRefreshExpiresAt,
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };
        await _refreshTokenRepository.AddAsync(newTokenEntity, cancellationToken);

        return new LoginResponseDto
        {
            Token = newJwtToken,
            AccessToken = newJwtToken,
            RefreshToken = newRefreshTokenString,
            ExpiresAt = tokenExpiresAt,
            RefreshExpiresAt = newRefreshExpiresAt,
            User = userInfo
        };
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var existingToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken, cancellationToken);
            if (existingToken != null)
            {
                existingToken.IsRevoked = true;
                await _refreshTokenRepository.UpdateAsync(existingToken, cancellationToken);
            }
        }
    }

    private static string GenerateRefreshTokenString()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
