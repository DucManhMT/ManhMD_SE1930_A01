using FUNews.BusinessLogic.Exceptions;

namespace FUNews.BusinessLogic.Helpers;

public static class NewsArticleValidationHelper
{
    public const int MaxIdLength = 20;
    public const int MaxHeadlineLength = 150;
    public const int MaxTitleLength = 400;
    public const int MaxContentLength = 4000;
    public const int MaxSourceLength = 400;

    public static string ValidateHeadline(string? headline, string fieldName = "Headline")
    {
        var trimmed = headline?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ValidationException(fieldName, "Tiêu đề tóm tắt (Headline) là bắt buộc.");
        }

        if (trimmed.Length > MaxHeadlineLength)
        {
            throw new ValidationException(fieldName, $"Tiêu đề tóm tắt không được vượt quá {MaxHeadlineLength} ký tự.");
        }

        return trimmed;
    }

    public static string? ValidateTitle(string? title, string fieldName = "NewsTitle")
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var trimmed = title.Trim();
        if (trimmed.Length > MaxTitleLength)
        {
            throw new ValidationException(fieldName, $"Tiêu đề bài viết không được vượt quá {MaxTitleLength} ký tự.");
        }

        return trimmed;
    }

    public static string? ValidateContent(string? content, string fieldName = "NewsContent")
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var trimmed = content.Trim();
        if (trimmed.Length > MaxContentLength)
        {
            throw new ValidationException(fieldName, $"Nội dung bài viết không được vượt quá {MaxContentLength} ký tự.");
        }

        return trimmed;
    }

    public static string? ValidateSource(string? source, string fieldName = "NewsSource")
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var trimmed = source.Trim();
        if (trimmed.Length > MaxSourceLength)
        {
            throw new ValidationException(fieldName, $"Nguồn tin không được vượt quá {MaxSourceLength} ký tự.");
        }

        return trimmed;
    }

    public static List<int> ValidateAndDeduplicateTags(IEnumerable<int>? tagIds)
    {
        if (tagIds == null)
        {
            return new List<int>();
        }

        return tagIds.Where(id => id > 0).Distinct().ToList();
    }
}
