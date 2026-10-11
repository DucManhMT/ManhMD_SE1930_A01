using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ManhMD_SE1930_A01_FE.Pages;

[ValidateAntiForgeryToken]
public class LoginModel : PageModel
{
    private readonly IAuthClientService _authClientService;

    public LoginModel(IAuthClientService authClientService)
    {
        _authClientService = authClientService ?? throw new ArgumentNullException(nameof(authClientService));
    }

    [BindProperty]
    [Required(ErrorMessage = "Vui lòng nhập địa chỉ email.")]
    [EmailAddress(ErrorMessage = "Địa chỉ email không đúng định dạng.")]
    [Display(Name = "Địa chỉ Email")]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public string? ReturnUrl { get; set; }

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToDefaultPage();
        }

        ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            ErrorMessage = "Vui lòng kiểm tra lại thông tin đăng nhập.";
            return Page();
        }

        try
        {
            var response = await _authClientService.LoginAsync(Email, Password, HttpContext.RequestAborted);

            // Xây dựng danh sách Claims an toàn
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, response.User.AccountName),
                new(ClaimTypes.Email, response.User.AccountEmail),
                new(ClaimTypes.Role, response.User.RoleName),
                new("jwt_token", response.Token)
            };

            // Tuân thủ Acceptance Criteria 7: Admin KHÔNG có AccountID trong DB và KHÔNG gán ID giả
            if (response.User.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, response.User.AccountEmail));
            }
            else if (response.User.AccountId.HasValue)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, response.User.AccountId.Value.ToString()));
                claims.Add(new Claim("accountId", response.User.AccountId.Value.ToString()));
                if (response.User.AccountRole.HasValue)
                {
                    claims.Add(new Claim("roleId", response.User.AccountRole.Value.ToString()));
                }
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = response.ExpiresAt
            };

            authProperties.StoreTokens(new[]
            {
                new AuthenticationToken { Name = "access_token", Value = response.Token }
            });

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

            // Lưu thông tin phiên làm việc phía server
            HttpContext.Session.SetString("jwt_token", response.Token);
            HttpContext.Session.SetString("user_role", response.User.RoleName);
            HttpContext.Session.SetString("user_name", response.User.AccountName);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToDefaultPage();
        }
        catch (FUNewsApiException ex)
        {
            ErrorMessage = ex.Message ?? "Email hoặc mật khẩu không chính xác.";
            Password = string.Empty; // Xóa mật khẩu khi gặp lỗi để bảo mật
            return Page();
        }
        catch (Exception)
        {
            ErrorMessage = "Không thể kết nối đến hệ thống máy chủ xác thực. Vui lòng thử lại sau.";
            Password = string.Empty;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAjaxLoginAsync([FromBody] LoginRequestApiModel request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new JsonResult(new { success = false, message = "Vui lòng nhập đầy đủ email và mật khẩu." });
        }

        try
        {
            var response = await _authClientService.LoginAsync(request.Email, request.Password, HttpContext.RequestAborted);

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, response.User.AccountName),
                new(ClaimTypes.Email, response.User.AccountEmail),
                new(ClaimTypes.Role, response.User.RoleName),
                new("jwt_token", response.Token)
            };

            if (response.User.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, response.User.AccountEmail));
            }
            else if (response.User.AccountId.HasValue)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, response.User.AccountId.Value.ToString()));
                claims.Add(new Claim("accountId", response.User.AccountId.Value.ToString()));
                if (response.User.AccountRole.HasValue)
                {
                    claims.Add(new Claim("roleId", response.User.AccountRole.Value.ToString()));
                }
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = response.ExpiresAt
            };

            authProperties.StoreTokens(new[]
            {
                new AuthenticationToken { Name = "access_token", Value = response.Token }
            });

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

            HttpContext.Session.SetString("jwt_token", response.Token);
            HttpContext.Session.SetString("user_role", response.User.RoleName);
            HttpContext.Session.SetString("user_name", response.User.AccountName);

            var redirectUrl = response.User.RoleName switch
            {
                "Admin" => "/Admin/Accounts",
                "Staff" => "/Staff/News",
                _ => "/Index"
            };

            return new JsonResult(new
            {
                success = true,
                message = "Đăng nhập thành công!",
                redirectUrl,
                user = new
                {
                    name = response.User.AccountName,
                    email = response.User.AccountEmail,
                    role = response.User.RoleName
                }
            });
        }
        catch (FUNewsApiException ex)
        {
            return new JsonResult(new { success = false, message = ex.Message ?? "Email hoặc mật khẩu không chính xác." });
        }
        catch (Exception)
        {
            return new JsonResult(new { success = false, message = "Không thể kết nối đến máy chủ xác thực. Vui lòng thử lại sau." });
        }
    }

    private IActionResult RedirectToDefaultPage()
    {
        return RedirectToPage("/Index");
    }
}
