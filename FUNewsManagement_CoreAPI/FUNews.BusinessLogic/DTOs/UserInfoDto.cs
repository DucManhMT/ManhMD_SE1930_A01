namespace FUNews.BusinessLogic.DTOs;

public class UserInfoDto
{
    /// <summary>
    /// Mã tài khoản trong CSDL SystemAccount.
    /// Đối với Quản trị viên (Admin cấu hình từ appsettings), trường này luôn luôn là null (không gán ID giả).
    /// </summary>
    public short? AccountId { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public string AccountEmail { get; set; } = string.Empty;

    public int? AccountRole { get; set; }

    /// <summary>
    /// Tên vai trò: "Admin", "Staff", hoặc "Lecturer".
    /// </summary>
    public string RoleName { get; set; } = string.Empty;
}
