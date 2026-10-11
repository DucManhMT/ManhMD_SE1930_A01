using System.Text.Json.Serialization;

namespace FUNews.Client.DataAccess.Models;

public class NewsArticleApiModel
{
    [JsonPropertyName("newsArticleId")]
    public string NewsArticleId { get; set; } = string.Empty;

    [JsonPropertyName("newsTitle")]
    public string? NewsTitle { get; set; }

    [JsonPropertyName("headline")]
    public string Headline { get; set; } = string.Empty;

    [JsonPropertyName("newsContent")]
    public string? NewsContent { get; set; }

    [JsonPropertyName("newsSource")]
    public string? NewsSource { get; set; }

    [JsonPropertyName("categoryId")]
    public short? CategoryId { get; set; }

    [JsonPropertyName("categoryName")]
    public string? CategoryName { get; set; }

    [JsonPropertyName("newsStatus")]
    public bool? NewsStatus { get; set; }

    [JsonPropertyName("createdById")]
    public short? CreatedById { get; set; }

    [JsonPropertyName("authorName")]
    public string? AuthorName { get; set; }

    [JsonPropertyName("createdDate")]
    public DateTime? CreatedDate { get; set; }

    [JsonPropertyName("updatedById")]
    public short? UpdatedById { get; set; }

    [JsonPropertyName("lastEditorName")]
    public string? LastEditorName { get; set; }

    [JsonPropertyName("modifiedDate")]
    public DateTime? ModifiedDate { get; set; }

    [JsonPropertyName("tags")]
    public List<TagApiModel> Tags { get; set; } = new();
}
