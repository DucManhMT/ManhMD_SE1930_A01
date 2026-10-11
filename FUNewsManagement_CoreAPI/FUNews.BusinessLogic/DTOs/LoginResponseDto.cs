namespace FUNews.BusinessLogic.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public UserInfoDto User { get; set; } = null!;
}
