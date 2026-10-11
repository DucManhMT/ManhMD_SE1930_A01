namespace FUNews.Client.DataAccess.Models;

public class TagSuggestionApiModel
{
    public int TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

public class TagSuggestionResponseApiModel
{
    public List<TagSuggestionApiModel> Tags { get; set; } = new();
}

public class TagFeedbackRequestApiModel
{
    public int TagId { get; set; }
    public string? ArticleId { get; set; }
}
