namespace FUNews.Client.DataAccess.Models;

public class AnalyticsDashboardApiModel
{
    public int TotalArticles { get; set; }
    public int ActiveArticles { get; set; }
    public int InactiveArticles { get; set; }
    public List<CategoryStatItem> ArticlesByCategory { get; set; } = new();
    public List<PeriodStatItem> ArticlesByPeriod { get; set; } = new();
}

public class CategoryStatItem
{
    public short CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class PeriodStatItem
{
    public string Period { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TrendingArticleApiModel
{
    public string NewsArticleId { get; set; } = string.Empty;
    public string NewsTitle { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? CategoryName { get; set; }
}

public class RecommendedArticleApiModel
{
    public string NewsArticleId { get; set; } = string.Empty;
    public string NewsTitle { get; set; } = string.Empty;
    public string? Headline { get; set; }
    public string? CategoryName { get; set; }
}
