namespace FUNews.DataAccess.Entities;

public class NewsView
{
    public long ViewID { get; set; }

    public string NewsArticleID { get; set; } = string.Empty;

    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

    public string? VisitorIP { get; set; }

    // Navigation property
    public virtual NewsArticle NewsArticle { get; set; } = null!;
}
