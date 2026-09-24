using System.Text.Json.Serialization;

namespace FUNews.Client.DataAccess.Models;

public class CategoryApiModel
{
    [JsonPropertyName("categoryId")]
    public short CategoryId { get; set; }

    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName("categoryDescription")]
    public string CategoryDescription { get; set; } = string.Empty;

    [JsonPropertyName("parentCategoryId")]
    public short? ParentCategoryId { get; set; }

    [JsonPropertyName("parentCategoryName")]
    public string? ParentCategoryName { get; set; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }
}
