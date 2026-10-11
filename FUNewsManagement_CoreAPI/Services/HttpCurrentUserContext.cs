using System.Security.Claims;
using FUNews.DataAccess.Context;
using Microsoft.AspNetCore.Http;

namespace ManhMD_SE1930_A01_BE.Services;

public class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public int? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var claimVal = user?.FindFirst("accountId")?.Value
                ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return short.TryParse(claimVal, out var id) ? id : null;
        }
    }

    public string? UserName
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.Email)?.Value
                ?? user?.FindFirst(ClaimTypes.Name)?.Value
                ?? user?.Identity?.Name
                ?? "System";
        }
    }
}
