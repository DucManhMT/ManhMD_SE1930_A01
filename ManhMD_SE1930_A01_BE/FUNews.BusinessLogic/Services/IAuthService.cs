using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Models;

namespace FUNews.BusinessLogic.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}
