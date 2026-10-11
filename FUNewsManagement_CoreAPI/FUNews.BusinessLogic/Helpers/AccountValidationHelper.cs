using FUNews.BusinessLogic.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FUNews.BusinessLogic.Helpers;

public static class AccountValidationHelper
{
    public const int MaxNameLength = 100;
    public const int MaxEmailLength = 70;
    public const int MinPasswordLength = 6;

    public static string ValidateAndTrimName(string? name, string fieldName = "AccountName")
    {
        var trimmedName = name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw new ValidationException(fieldName, "Họ và tên là bắt buộc.");
        }
        if (trimmedName.Length > MaxNameLength)
        {
            throw new ValidationException(fieldName, $"Họ và tên không được vượt quá {MaxNameLength} ký tự.");
        }
        return trimmedName;
    }

    public static string ValidateAndTrimEmail(string? email, string fieldName = "AccountEmail")
    {
        var trimmedEmail = email?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedEmail))
        {
            throw new ValidationException(fieldName, "Email là bắt buộc.");
        }
        if (trimmedEmail.Length > MaxEmailLength)
        {
            throw new ValidationException(fieldName, $"Email không được vượt quá {MaxEmailLength} ký tự.");
        }
        return trimmedEmail;
    }

    public static int ValidateRole(int? role, string fieldName = "AccountRole")
    {
        if (!role.HasValue || (role.Value != 1 && role.Value != 2))
        {
            throw new ValidationException(fieldName, "Vai trò không hợp lệ. Chỉ chấp nhận Nhân viên (1) hoặc Giảng viên (2).");
        }
        return role.Value;
    }

    public static void ValidatePassword(string? password, string fieldName = "AccountPassword")
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
        {
            throw new ValidationException(fieldName, $"Mật khẩu phải có độ dài tối thiểu {MinPasswordLength} ký tự.");
        }
    }

    public static void HandleDbUpdateException(DbUpdateException ex, string emailFieldName = "AccountEmail")
    {
        var innerMsg = ex.InnerException?.Message ?? ex.Message;
        var isEmailUniqueViolation =
            innerMsg.Contains("UQ_SystemAccount_AccountEmail", StringComparison.OrdinalIgnoreCase)
            || (innerMsg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                && innerMsg.Contains("AccountEmail", StringComparison.OrdinalIgnoreCase));

        if (isEmailUniqueViolation)
        {
            throw new ValidationException(
                emailFieldName,
                "Email này đã được sử dụng bởi một tài khoản khác trong hệ thống.");
        }

        // For all other DbUpdateExceptions (PK collision, FK, etc.), re-throw
        // so ExceptionHandlingMiddleware returns 409 Conflict.
        throw ex;
    }
}
