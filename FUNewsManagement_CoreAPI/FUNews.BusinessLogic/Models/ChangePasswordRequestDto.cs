using System.ComponentModel.DataAnnotations;

namespace FUNews.BusinessLogic.Models;

public class ChangePasswordRequestDto
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
