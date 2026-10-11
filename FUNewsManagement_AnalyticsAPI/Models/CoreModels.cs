namespace FUNewsManagement_AnalyticsAPI.Models;

public class CoreODataResponse<T>
{
    public List<T> Value { get; set; } = new();
    public int? Count { get; set; }
}

public class CoreNewsArticleDto
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
    public string? NewsImage { get; set; }
    public List<CoreTagDto> Tags { get; set; } = new();
}

public class CoreTagDto
{
    public int TagId { get; set; }
    public string? TagName { get; set; }
    public string? Note { get; set; }
}

public class CoreCategoryDto
{
    public short CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryDescription { get; set; }
    public short? ParentCategoryId { get; set; }
    public bool? IsActive { get; set; }
    public int ArticleCount { get; set; }
}
