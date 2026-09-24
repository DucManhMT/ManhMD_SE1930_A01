using System.Text.Json.Serialization;

namespace FUNews.Client.DataAccess.Models;

public class AccountApiModel
{
    [JsonPropertyName("accountId")]
    public short AccountId { get; set; }

    [JsonPropertyName("accountName")]
    public string? AccountName { get; set; }

    [JsonPropertyName("accountEmail")]
    public string? AccountEmail { get; set; }

    [JsonPropertyName("accountRole")]
    public int? AccountRole { get; set; }

    [JsonPropertyName("roleName")]
    public string? RoleName { get; set; }
}
