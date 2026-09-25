namespace FUNews.Client.DataAccess.Models;

public class ReportDetailApiModel
{
    public string NewsArticleId { get; set; } = string.Empty;
    public string? NewsTitle { get; set; }
    public string Headline { get; set; } = string.Empty;
    public short? CategoryId { get; set; }
    public string CategoryName { get; set; } = "Không phân loại";
    public short? CreatedById { get; set; }
    public string AuthorName { get; set; } = "Không xác định";
    public DateTime? CreatedDate { get; set; }
    public bool? NewsStatus { get; set; }
    public short? UpdatedById { get; set; }
    public string? LastEditorName { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

public class ReportGroupSummaryApiModel
{
    public string GroupKey { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
}

public class NewsReportApiModel
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string GroupBy { get; set; } = "category";
    public int TotalArticles { get; set; }
    public int TotalActive { get; set; }
    public int TotalInactive { get; set; }
    public List<ReportGroupSummaryApiModel> GroupSummaries { get; set; } = new();
    public List<ReportDetailApiModel> Details { get; set; } = new();
}
