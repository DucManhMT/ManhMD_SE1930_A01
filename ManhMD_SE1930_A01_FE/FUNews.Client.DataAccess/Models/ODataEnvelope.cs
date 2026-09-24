using System.Text.Json.Serialization;

namespace FUNews.Client.DataAccess.Models;

public class ODataEnvelope<T>
{
    [JsonPropertyName("@odata.count")]
    public long? Count { get; set; }

    [JsonPropertyName("value")]
    public List<T> Value { get; set; } = new();
}
