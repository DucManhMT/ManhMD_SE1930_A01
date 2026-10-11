namespace FUNews.BusinessLogic.DTOs;

public class NewsArticleListDto
{
    public string NewsArticleId { get; set; } = string.Empty;
    public string? NewsTitle { get; set; }
    public string Headline { get; set; } = string.Empty;
    public short? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public short? AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public bool? NewsStatus { get; set; }
    public DateTime? CreatedDate { get; set; }
    public List<string> Tags { get; set; } = new();
}
