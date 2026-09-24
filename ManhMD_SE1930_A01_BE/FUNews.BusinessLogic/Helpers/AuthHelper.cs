using FUNews.BusinessLogic.DTOs;
using FUNews.BusinessLogic.Exceptions;
using FUNews.BusinessLogic.Options;
using FUNews.DataAccess.Entities;

namespace FUNews.BusinessLogic.Helpers;

public static class AuthHelper
{
    public static (string trimmedEmail, string plainPassword) ValidateAndExtractCredentials(string? email, string? password)
    {
        var trimmedEmail = email?.Trim() ?? string.Empty;
        var plainPassword = password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedEmail) || string.IsNullOrWhiteSpace(plainPassword))
        {
            throw new UnauthorizedException("Email hoặc mật khẩu không chính xác.");
        }

        return (trimmedEmail, plainPassword);
    }

    public static UserInfoDto CreateAdminUserInfo(string adminEmail)
    {
        return new UserInfoDto
        {
            AccountId = null, // Ràng buộc bắt buộc: Admin không có bản ghi trong SystemAccount và không gán ID giả
            AccountName = "Quản trị viên",
            AccountEmail = adminEmail,
            AccountRole = null,
            RoleName = "Admin"
        };
    }

    public static UserInfoDto CreateAccountUserInfo(SystemAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        string roleName = account.AccountRole switch
        {
            1 => "Staff",
            2 => "Lecturer",
            _ => throw new ForbiddenException("Tài khoản không có quyền truy cập hệ thống.")
        };

        return new UserInfoDto
        {
            AccountId = account.AccountID,
            AccountName = account.AccountName ?? string.Empty,
            AccountEmail = account.AccountEmail ?? string.Empty,
            AccountRole = account.AccountRole,
            RoleName = roleName
        };
    }
}
