using System.ComponentModel.DataAnnotations;

namespace FUNews.Client.DataAccess.Models;

public class LoginRequestApiModel
{
    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(70, ErrorMessage = "Email không được vượt quá 70 ký tự.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    public string Password { get; set; } = string.Empty;
}
