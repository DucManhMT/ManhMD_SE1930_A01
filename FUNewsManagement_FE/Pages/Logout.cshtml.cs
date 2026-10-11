using FUNews.Client.DataAccess.Clients;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ManhMD_SE1930_A01_FE.Pages;

[ValidateAntiForgeryToken]
public class LogoutModel : PageModel
{
    private readonly ICoreApiClient _coreApiClient;
    private readonly ITokenProvider _tokenProvider;

    public LogoutModel(ICoreApiClient coreApiClient, ITokenProvider tokenProvider)
    {
        _coreApiClient = coreApiClient ?? throw new ArgumentNullException(nameof(coreApiClient));
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
    }

    public IActionResult OnGet()
    {
        // Điều hướng an toàn nếu truy cập bằng GET
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var refreshToken = _tokenProvider.GetRefreshToken();
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            try
            {
                await _coreApiClient.LogoutAsync(refreshToken, HttpContext.RequestAborted);
            }
            catch
            {
                // Cho phép đăng xuất cục bộ kể cả khi không thể kết nối tới Core API
            }
        }

        // Xóa Cookie Authentication
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Hủy toàn bộ dữ liệu Session và Tokens phía server
        _tokenProvider.ClearTokens();
        HttpContext.Session.Clear();

        return RedirectToPage("/Login");
    }
}
