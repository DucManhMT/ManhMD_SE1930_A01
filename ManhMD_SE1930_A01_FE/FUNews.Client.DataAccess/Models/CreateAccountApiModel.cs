using System.Text.Json.Serialization;

namespace FUNews.Client.DataAccess.Models;

public class CreateAccountApiModel
{
    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = string.Empty;

    [JsonPropertyName("accountEmail")]
    public string AccountEmail { get; set; } = string.Empty;

    [JsonPropertyName("accountRole")]
    public int? AccountRole { get; set; }

    [JsonPropertyName("accountPassword")]
    public string AccountPassword { get; set; } = string.Empty;
}
