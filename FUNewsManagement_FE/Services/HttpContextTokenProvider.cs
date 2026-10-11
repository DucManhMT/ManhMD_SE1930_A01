using FUNews.Client.DataAccess.Clients;

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

        return httpContext.User.FindFirst("jwt_token")?.Value
            ?? httpContext.Session?.GetString("jwt_token");
    }
}
