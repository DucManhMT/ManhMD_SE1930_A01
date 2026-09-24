using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ManhMD_SE1930_A01_FE.Helpers;

public static class ValidationResponseHelper
{
    public static string NormalizePropertyName(string propertyName)
    {
        var clean = propertyName.Replace("input.", "", StringComparison.OrdinalIgnoreCase)
                                .Replace("Input.", "", StringComparison.OrdinalIgnoreCase)
                                .Replace("editInput.", "", StringComparison.OrdinalIgnoreCase);

        if (clean.Equals("AccountEmail", StringComparison.OrdinalIgnoreCase) || clean.Equals("email", StringComparison.OrdinalIgnoreCase))
            return "AccountEmail";
        if (clean.Equals("AccountName", StringComparison.OrdinalIgnoreCase) || clean.Equals("name", StringComparison.OrdinalIgnoreCase))
            return "AccountName";
        if (clean.Equals("AccountRole", StringComparison.OrdinalIgnoreCase) || clean.Equals("role", StringComparison.OrdinalIgnoreCase))
            return "AccountRole";
        if (clean.Equals("AccountPassword", StringComparison.OrdinalIgnoreCase) || clean.Equals("password", StringComparison.OrdinalIgnoreCase))
            return "AccountPassword";

        return clean;
    }

    public static Dictionary<string, string[]> ExtractModelStateErrors(ModelStateDictionary modelState)
    {
        return modelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                k => NormalizePropertyName(k.Key),
                v => v.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );
    }

    public static Dictionary<string, string[]> NormalizeApiErrors(IDictionary<string, string[]>? validationErrors)
    {
        var normalizedErrors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (validationErrors != null)
        {
            foreach (var kvp in validationErrors)
            {
                normalizedErrors[NormalizePropertyName(kvp.Key)] = kvp.Value;
            }
        }
        return normalizedErrors;
    }
}
