using FUNews.BusinessLogic.DTOs;

namespace FUNews.BusinessLogic.Security;

public interface IJwtTokenService
{
    string GenerateToken(UserInfoDto user, out DateTime expiresAt);
}
