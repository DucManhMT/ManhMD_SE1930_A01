namespace FUNews.Client.DataAccess.Models;

public class LoginResponseApiModel
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public UserInfoApiModel User { get; set; } = null!;
}
