using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Models;

namespace FUNews.BusinessLogic.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    Task<LoginResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}
