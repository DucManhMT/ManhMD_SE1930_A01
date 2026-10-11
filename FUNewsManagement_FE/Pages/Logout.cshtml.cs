using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ManhMD_SE1930_A01_FE.Pages;

[ValidateAntiForgeryToken]
public class LogoutModel : PageModel
{
    public IActionResult OnGet()
    {
        // Điều hướng an toàn nếu truy cập bằng GET
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Xóa Cookie Authentication
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Hủy toàn bộ dữ liệu Session phía server
        HttpContext.Session.Clear();

        return RedirectToPage("/Login");
    }
}
