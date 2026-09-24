using System.Text.Json.Serialization;

namespace FUNews.Client.DataAccess.Models;

public class UpdateNewsArticleApiModel
{
    [JsonPropertyName("newsTitle")]
    public string? NewsTitle { get; set; }

    [JsonPropertyName("headline")]
    public string Headline { get; set; } = string.Empty;

    [JsonPropertyName("newsContent")]
    public string? NewsContent { get; set; }

    [JsonPropertyName("newsSource")]
    public string? NewsSource { get; set; }

    [JsonPropertyName("categoryId")]
    public short CategoryId { get; set; }

    [JsonPropertyName("newsStatus")]
    public bool? NewsStatus { get; set; } = true;

    [JsonPropertyName("tagIds")]
    public List<int> TagIds { get; set; } = new();
}
