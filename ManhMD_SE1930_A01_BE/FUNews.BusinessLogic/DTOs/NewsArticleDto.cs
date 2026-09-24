namespace FUNews.BusinessLogic.DTOs;

public class NewsArticleDto
{
    public string NewsArticleId { get; set; } = string.Empty;
    public string? NewsTitle { get; set; }
    public string Headline { get; set; } = string.Empty;
    public string? NewsContent { get; set; }
    public string? NewsSource { get; set; }
    public short? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool? NewsStatus { get; set; }
    public short? CreatedById { get; set; }
    public string? AuthorName { get; set; }
    public DateTime? CreatedDate { get; set; }
    public short? UpdatedById { get; set; }
    public string? LastEditorName { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public List<TagDto> Tags { get; set; } = new();
}
