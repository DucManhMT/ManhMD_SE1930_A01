using FUNews.Client.DataAccess.Clients;
using Microsoft.AspNetCore.Http;

namespace ManhMD_SE1930_A01_FE.Services;

public class HttpContextTokenProvider : ITokenProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextTokenProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public string? GetToken()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return null;

        return httpContext.Session?.GetString("access_token")
            ?? httpContext.User.FindFirst("jwt_token")?.Value
            ?? httpContext.Session?.GetString("jwt_token");
    }

    public string? GetRefreshToken()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return null;

        return httpContext.Session?.GetString("refresh_token");
    }

    public void SaveTokens(string accessToken, string refreshToken, DateTime? expiresAt = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Session != null)
        {
            httpContext.Session.SetString("access_token", accessToken);
            httpContext.Session.SetString("jwt_token", accessToken);
            httpContext.Session.SetString("refresh_token", refreshToken);
            if (expiresAt.HasValue)
            {
                httpContext.Session.SetString("token_expires_at", expiresAt.Value.ToString("O"));
            }
        }
    }

    public void ClearTokens()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Session != null)
        {
            httpContext.Session.Remove("access_token");
            httpContext.Session.Remove("jwt_token");
            httpContext.Session.Remove("refresh_token");
            httpContext.Session.Remove("token_expires_at");
        }
    }
}
