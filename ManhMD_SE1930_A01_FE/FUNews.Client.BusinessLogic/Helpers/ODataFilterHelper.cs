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
}
