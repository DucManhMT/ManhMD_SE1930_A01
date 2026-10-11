namespace FUNews.Client.DataAccess.Clients;

public interface ITokenProvider
{
    string? GetToken();

    string? GetRefreshToken();

    void SaveTokens(string accessToken, string refreshToken, DateTime? expiresAt = null);

    void ClearTokens();
}
