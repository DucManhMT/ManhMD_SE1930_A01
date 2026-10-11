using System.Text.Json.Serialization;

namespace FUNews.Client.DataAccess.Models;

public class CreateTagApiModel
{
    [JsonPropertyName("tagName")]
    public string TagName { get; set; } = string.Empty;

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}
