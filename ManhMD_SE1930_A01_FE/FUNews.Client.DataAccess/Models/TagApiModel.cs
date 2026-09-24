using System.Text.Json.Serialization;

namespace FUNews.Client.DataAccess.Models;

public class TagApiModel
{
    [JsonPropertyName("tagId")]
    public int TagId { get; set; }

    [JsonPropertyName("tagName")]
    public string? TagName { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("articleCount")]
    public int ArticleCount { get; set; }
}
