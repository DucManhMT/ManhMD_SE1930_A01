namespace FUNews.BusinessLogic.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;

    public string AccessToken
    {
        get => Token;
        set => Token = value;
    }

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RefreshExpiresAt { get; set; }

    public UserInfoDto User { get; set; } = null!;
}
