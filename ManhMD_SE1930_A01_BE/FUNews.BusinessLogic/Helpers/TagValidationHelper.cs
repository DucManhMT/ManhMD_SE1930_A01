using FUNews.BusinessLogic.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FUNews.BusinessLogic.Helpers;

public static class TagValidationHelper
{
    public const int MaxNameLength = 50;
    public const int MaxNoteLength = 400;
    public const string DuplicateNameMessage = "Tên thẻ tin đã tồn tại trong hệ thống.";

    public static string ValidateAndTrimName(string? name, string fieldName = "TagName")
    {
        var trimmedName = name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw new ValidationException(fieldName, "Tên thẻ tin là bắt buộc.");
        }

        if (trimmedName.Length > MaxNameLength)
        {
            throw new ValidationException(fieldName, $"Tên thẻ tin không được vượt quá {MaxNameLength} ký tự.");
        }

        return trimmedName;
    }

    public static string? ValidateAndTrimNote(string? note, string fieldName = "Note")
    {
        var trimmedNote = note?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedNote))
        {
            return null;
        }

        if (trimmedNote.Length > MaxNoteLength)
        {
            throw new ValidationException(fieldName, $"Ghi chú thẻ tin không được vượt quá {MaxNoteLength} ký tự.");
        }

        return trimmedNote;
    }

    public static void HandleDbUpdateException(DbUpdateException ex, string fieldName = "TagName", string message = DuplicateNameMessage)
    {
        var innerMsg = ex.InnerException?.Message ?? ex.Message;
        var isUniqueViolation = innerMsg.Contains("UQ_Tag_TagName", StringComparison.OrdinalIgnoreCase)
            || (innerMsg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                && innerMsg.Contains("TagName", StringComparison.OrdinalIgnoreCase));

        if (isUniqueViolation)
        {
            throw new ValidationException(fieldName, message);
        }

        throw ex;
    }
}
