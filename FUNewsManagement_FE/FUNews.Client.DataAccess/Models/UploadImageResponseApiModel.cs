using System.Text.Json.Serialization;

namespace FUNews.Client.DataAccess.Models;

public class UploadImageResponseApiModel
{
    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;
}
