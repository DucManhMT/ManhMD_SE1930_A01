using System.Text.Json.Serialization;

namespace FUNews.BusinessLogic.DTOs;

public class ODataResponse<T>
{
    [JsonPropertyName("@odata.count")]
    public long? Count { get; set; }

    [JsonPropertyName("value")]
    public List<T> Value { get; set; } = new();
}
