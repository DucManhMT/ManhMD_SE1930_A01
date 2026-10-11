using System.ComponentModel.DataAnnotations;

namespace FUNews.BusinessLogic.Models;

public class CreateAccountRequestDto
{
    [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
    [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
    public string AccountName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(70, ErrorMessage = "Email không được vượt quá 70 ký tự.")]
    public string AccountEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vai trò là bắt buộc.")]
    [Range(1, 2, ErrorMessage = "Vai trò không hợp lệ. Chỉ chấp nhận Nhân viên (1) hoặc Giảng viên (2).")]
    public int? AccountRole { get; set; }

    [Required(ErrorMessage = "Mật khẩu khởi tạo là bắt buộc.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 100 ký tự.")]
    public string AccountPassword { get; set; } = string.Empty;
}
