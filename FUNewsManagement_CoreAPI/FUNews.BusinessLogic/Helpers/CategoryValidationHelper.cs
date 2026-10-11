using FUNews.BusinessLogic.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FUNews.BusinessLogic.Helpers;

public static class CategoryValidationHelper
{
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 250;
    public const string CreateDuplicateNameMessage = "Tên danh mục đã tồn tại trong cùng danh mục cha.";
    public const string DuplicateNameMessage = "Tên chuyên mục đã tồn tại trong cùng danh mục cha.";

    public static string ValidateAndTrimName(string? name, string fieldName = "CategoryName")
    {
        var trimmedName = name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw new ValidationException(fieldName, "Tên chuyên mục là bắt buộc.");
        }

        if (trimmedName.Length > MaxNameLength)
        {
            throw new ValidationException(fieldName, $"Tên chuyên mục không được vượt quá {MaxNameLength} ký tự.");
        }

        return trimmedName;
    }

    public static string ValidateAndTrimDescription(string? desc, string fieldName = "CategoryDescription")
    {
        var trimmedDesc = desc?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedDesc))
        {
            throw new ValidationException(fieldName, "Mô tả chuyên mục là bắt buộc.");
        }

        if (trimmedDesc.Length > MaxDescriptionLength)
        {
            throw new ValidationException(fieldName, $"Mô tả chuyên mục không được vượt quá {MaxDescriptionLength} ký tự.");
        }

        return trimmedDesc;
    }

    public static void ValidateParentNotSelf(short? parentId, short categoryId, string fieldName = "ParentCategoryId")
    {
        if (parentId.HasValue && parentId.Value == categoryId)
        {
            throw new ValidationException(fieldName, "Chuyên mục không thể chọn chính mình làm danh mục cha.");
        }
    }

    public static void HandleDbUpdateException(DbUpdateException ex, string fieldName = "CategoryName", string message = DuplicateNameMessage)
    {
        var innerMsg = ex.InnerException?.Message ?? ex.Message;
        var isUniqueViolation = innerMsg.Contains("UQ_Category_Name", StringComparison.OrdinalIgnoreCase)
            || (innerMsg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                && innerMsg.Contains("CategoryName", StringComparison.OrdinalIgnoreCase));

        if (isUniqueViolation)
        {
            throw new ValidationException(fieldName, message);
        }

        throw ex;
    }
}
