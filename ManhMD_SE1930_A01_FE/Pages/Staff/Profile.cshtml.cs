using System.ComponentModel.DataAnnotations;
using FUNews.Client.BusinessLogic.Services;
using FUNews.Client.DataAccess.Exceptions;
using FUNews.Client.DataAccess.Models;
using ManhMD_SE1930_A01_FE.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ManhMD_SE1930_A01_FE.Pages.Staff;

[Authorize(Roles = "Staff")]
[ValidateAntiForgeryToken]
public class ProfileModel : PageModel
{
    private readonly IAccountClientService _accountClientService;
    private readonly ILogger<ProfileModel> _logger;

    public ProfileModel(IAccountClientService accountClientService, ILogger<ProfileModel> logger)
    {
        _accountClientService = accountClientService ?? throw new ArgumentNullException(nameof(accountClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public AccountApiModel? Profile { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            Profile = await _accountClientService.GetProfileAsync(HttpContext.RequestAborted);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load staff profile.");
            ErrorMessage = "Không thể tải thông tin hồ sơ từ máy chủ. Vui lòng thử lại sau.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostUpdateProfileAsync([FromBody] UpdateProfileInputModel input)
    {
        if (input == null)
        {
            return new JsonResult(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });
        }

        if (!ModelState.IsValid)
        {
            return new JsonResult(new
            {
                success = false,
                message = "Vui lòng kiểm tra lại thông tin nhập liệu.",
                errors = ValidationResponseHelper.ExtractModelStateErrors(ModelState)
            });
        }

        try
        {
            var apiModel = new UpdateProfileApiModel
            {
                AccountName = input.AccountName.Trim(),
                AccountEmail = input.AccountEmail.Trim()
            };

            var updatedProfile = await _accountClientService.UpdateProfileAsync(apiModel, HttpContext.RequestAborted);

            return new JsonResult(new
            {
                success = true,
                message = "Cập nhật thông tin hồ sơ thành công!",
                profile = updatedProfile
            });
        }
        catch (FUNewsApiException ex)
        {
            _logger.LogWarning(ex, "API error while updating staff profile: {Message}", ex.Message);
            return new JsonResult(new
            {
                success = false,
                message = ex.Message ?? "Không thể cập nhật hồ sơ.",
                errors = ValidationResponseHelper.NormalizeApiErrors(ex.ValidationErrors)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error while updating staff profile.");
            return new JsonResult(new
            {
                success = false,
                message = "Không thể kết nối đến máy chủ Backend API. Vui lòng thử lại sau."
            });
        }
    }

    public async Task<IActionResult> OnPostChangePasswordAsync([FromBody] ChangePasswordInputModel input)
    {
        if (input == null)
        {
            return new JsonResult(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });
        }

        if (!ModelState.IsValid)
        {
            return new JsonResult(new
            {
                success = false,
                message = "Vui lòng kiểm tra lại thông tin mật khẩu.",
                errors = ValidationResponseHelper.ExtractModelStateErrors(ModelState)
            });
        }

        try
        {
            var apiModel = new ChangePasswordApiModel
            {
                CurrentPassword = input.CurrentPassword,
                NewPassword = input.NewPassword,
                ConfirmPassword = input.ConfirmPassword
            };

            await _accountClientService.ChangePasswordAsync(apiModel, HttpContext.RequestAborted);

            return new JsonResult(new
            {
                success = true,
                message = "Đổi mật khẩu thành công! Vui lòng sử dụng mật khẩu mới cho các lần đăng nhập tiếp theo."
            });
        }
        catch (FUNewsApiException ex)
        {
            // Bảo đảm Acceptance Criterion 5: TUYỆT ĐỐI KHÔNG log mật khẩu
            _logger.LogWarning("API validation error while changing staff password: {Message}", ex.Message);

            return new JsonResult(new
            {
                success = false,
                message = ex.Message ?? "Không thể đổi mật khẩu.",
                errors = ValidationResponseHelper.NormalizeApiErrors(ex.ValidationErrors)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error while changing staff password.");
            return new JsonResult(new
            {
                success = false,
                message = "Không thể kết nối đến máy chủ Backend API. Vui lòng thử lại sau."
            });
        }
    }

    public class UpdateProfileInputModel
    {
        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
        public string AccountName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự.")]
        public string AccountEmail { get; set; } = string.Empty;
    }

    public class ChangePasswordInputModel
    {
        [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải có độ dài từ 6 đến 100 ký tự.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu mới là bắt buộc.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Xác nhận mật khẩu không khớp với mật khẩu mới.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
