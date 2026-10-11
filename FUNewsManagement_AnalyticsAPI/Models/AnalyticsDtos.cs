using System.Text.Json.Serialization;

namespace FUNewsManagement_AnalyticsAPI.Models;

public class CategoryStatItemDto
{
    public short CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class StatusStatItemDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class PeriodStatItemDto
{
    public string Period { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class AnalyticsDashboardDto
{
    public int TotalArticles { get; set; }
    public int ActiveArticles { get; set; }
    public int InactiveArticles { get; set; }
    public List<CategoryStatItemDto> ArticlesByCategory { get; set; } = new();
    
    // Alias to support contract in docs/a02/tasks/A02-008.md
    [JsonPropertyName("byCategory")]
    public List<CategoryStatItemDto> ByCategory
    {
        get => ArticlesByCategory;
        set => ArticlesByCategory = value;
    }

    [JsonPropertyName("byStatus")]
    public List<StatusStatItemDto> ByStatus { get; set; } = new();

    public List<PeriodStatItemDto> ArticlesByPeriod { get; set; } = new();
}

public class TrendingArticleDto
{
    public string NewsArticleId { get; set; } = string.Empty;
    public string NewsTitle { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? CategoryName { get; set; }
}

public class RecommendedArticleDto
{
    public string NewsArticleId { get; set; } = string.Empty;
    public string NewsTitle { get; set; } = string.Empty;
    public string? Headline { get; set; }
    public string? CategoryName { get; set; }
}

public class DashboardFilterQuery
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public short? CategoryId { get; set; }
    public short? AuthorId { get; set; }
    public bool? Status { get; set; }
}
