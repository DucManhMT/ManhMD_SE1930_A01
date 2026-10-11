namespace FUNews.Client.BusinessLogic.Helpers;

public static class ODataFilterHelper
{
    public static string EscapeStringLiteral(string value)
    {
        return value.Trim().Replace("'", "''");
    }

    public static string BuildEmailUniquenessQuery(string email, short? excludeId = null)
    {
        var escapedEmail = EscapeStringLiteral(email.ToLowerInvariant());
        var filter = $"accountEmail eq '{escapedEmail}'";
        if (excludeId.HasValue)
        {
            filter += $" and accountId ne {excludeId.Value}";
        }
        return $"$filter={filter}&$top=1&$count=true";
    }

    public static string BuildAccountsQuery(string? searchTerm, int? roleFilter, int top = 100)
    {
        var filters = new List<string>();

        if (roleFilter.HasValue && (roleFilter.Value == 1 || roleFilter.Value == 2))
        {
            filters.Add($"accountRole eq {roleFilter.Value}");
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var safeTerm = EscapeStringLiteral(searchTerm);
            filters.Add($"(contains(accountName,'{safeTerm}') or contains(accountEmail,'{safeTerm}'))");
        }

        var queryParts = new List<string>
        {
            "$orderby=accountId asc",
            "$count=true",
            $"$top={top}"
        };

        if (filters.Count > 0)
        {
            queryParts.Insert(0, $"$filter={string.Join(" and ", filters)}");
        }

        return string.Join("&", queryParts);
    }

    public static string BuildCategoriesQuery(string? searchTerm, string? statusFilter, int top = 100)
    {
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            if (string.Equals(statusFilter, "active", StringComparison.OrdinalIgnoreCase))
            {
                filters.Add("isActive eq true");
            }
            else if (string.Equals(statusFilter, "inactive", StringComparison.OrdinalIgnoreCase))
            {
                filters.Add("isActive eq false");
            }
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var safeTerm = EscapeStringLiteral(searchTerm);
            filters.Add($"(contains(categoryName,'{safeTerm}') or contains(categoryDescription,'{safeTerm}'))");
        }

        var queryParts = new List<string>
        {
            "$orderby=categoryId asc",
            "$count=true",
            $"$top={top}"
        };

        if (filters.Count > 0)
        {
            queryParts.Insert(0, $"$filter={string.Join(" and ", filters)}");
        }

        return string.Join("&", queryParts);
    }

    public static string BuildTagsQuery(string? searchTerm, int top = 100)
    {
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var safeTerm = EscapeStringLiteral(searchTerm);
            filters.Add($"(contains(tagName,'{safeTerm}') or (note ne null and contains(note,'{safeTerm}')))");
        }

        var queryParts = new List<string>
        {
            "$orderby=tagId desc",
            "$count=true",
            $"$top={top}"
        };

        if (filters.Count > 0)
        {
            queryParts.Insert(0, $"$filter={string.Join(" and ", filters)}");
        }

        return string.Join("&", queryParts);
    }

    public static string BuildNewsQuery(
        string? searchTerm = null,
        short? categoryId = null,
        string? statusFilter = null,
        string? authorTerm = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? sortBy = null,
        int top = 10,
        int skip = 0,
        int? tagId = null,
        bool includeContent = false)
    {
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var safeTerm = EscapeStringLiteral(searchTerm);
            if (includeContent)
            {
                filters.Add($"(contains(newsTitle,'{safeTerm}') or contains(headline,'{safeTerm}') or (newsContent ne null and contains(newsContent,'{safeTerm}')))");
            }
            else
            {
                filters.Add($"(contains(newsTitle,'{safeTerm}') or contains(headline,'{safeTerm}'))");
            }
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            filters.Add($"categoryId eq {categoryId.Value}");
        }

        if (tagId.HasValue && tagId.Value > 0)
        {
            filters.Add($"tags/any(t: t/tagId eq {tagId.Value})");
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            if (string.Equals(statusFilter, "active", StringComparison.OrdinalIgnoreCase))
            {
                filters.Add("newsStatus eq true");
            }
            else if (string.Equals(statusFilter, "inactive", StringComparison.OrdinalIgnoreCase))
            {
                filters.Add("newsStatus eq false");
            }
        }

        if (!string.IsNullOrWhiteSpace(authorTerm))
        {
            var safeAuthor = EscapeStringLiteral(authorTerm);
            filters.Add($"(authorName ne null and contains(authorName,'{safeAuthor}'))");
        }

        // AC 3: Date inclusive
        if (startDate.HasValue)
        {
            var startIso = startDate.Value.Date.ToString("yyyy-MM-ddTHH:mm:ssZ");
            filters.Add($"createdDate ge {startIso}");
        }

        if (endDate.HasValue)
        {
            var nextDayIso = endDate.Value.Date.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ");
            filters.Add($"createdDate lt {nextDayIso}");
        }

        // AC 4: Sort ổn định
        string orderbyClause = (sortBy?.ToLowerInvariant()) switch
        {
            "date_asc" => "$orderby=createdDate asc,newsArticleId asc",
            "title_asc" => "$orderby=newsTitle asc,newsArticleId asc",
            "title_desc" => "$orderby=newsTitle desc,newsArticleId desc",
            _ => "$orderby=createdDate desc,newsArticleId desc"
        };

        var queryParts = new List<string>
        {
            orderbyClause,
            "$count=true",
            $"$top={Math.Max(1, top)}"
        };

        if (skip > 0)
        {
            queryParts.Add($"$skip={skip}");
        }

        if (filters.Count > 0)
        {
            queryParts.Insert(0, $"$filter={string.Join(" and ", filters)}");
        }

        return string.Join("&", queryParts);
    }
}
