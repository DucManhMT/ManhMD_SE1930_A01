using System.Text.Json.Serialization;

namespace FUNews.Client.DataAccess.Models;

public class UpdateCategoryApiModel
{
    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [JsonPropertyName("categoryDescription")]
    public string CategoryDescription { get; set; } = string.Empty;

    [JsonPropertyName("parentCategoryId")]
    public short? ParentCategoryId { get; set; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }
}
