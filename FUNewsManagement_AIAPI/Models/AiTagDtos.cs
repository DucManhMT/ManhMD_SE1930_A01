using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FUNewsManagement_AIAPI.Models;

public class AiTagRequestDto
{
    [Required(ErrorMessage = "Nội dung bài viết không được để trống.")]
    public string Content { get; set; } = string.Empty;
}

public class TagSuggestionDto
{
    public int TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

public class TagSuggestionResponseDto
{
    [JsonPropertyName("suggestedTags")]
    public List<TagSuggestionDto> SuggestedTags { get; set; } = new();

    // Alias to match FE TagSuggestionResponseApiModel
    [JsonPropertyName("tags")]
    public List<TagSuggestionDto> Tags
    {
        get => SuggestedTags;
        set => SuggestedTags = value;
    }
}

public class TagFeedbackRequestDto
{
    public int? TagId { get; set; }
    public List<int>? SelectedTagIds { get; set; }
    public string? ArticleId { get; set; }

    public List<int> GetTagIds()
    {
        var list = new List<int>();
        if (SelectedTagIds != null && SelectedTagIds.Count > 0)
        {
            list.AddRange(SelectedTagIds);
        }
        if (TagId.HasValue && !list.Contains(TagId.Value))
        {
            list.Add(TagId.Value);
        }
        return list;
    }
}

public class TagFeedbackResponseDto
{
    public string Message { get; set; } = "Feedback recorded in Learning Cache";
    public List<int> RecordedTagIds { get; set; } = new();
}

public class CoreODataResponse<T>
{
    public List<T> Value { get; set; } = new();
    public int? Count { get; set; }
}

public class CoreTagDto
{
    public int TagId { get; set; }
    public string? TagName { get; set; }
    public string? Note { get; set; }
}
