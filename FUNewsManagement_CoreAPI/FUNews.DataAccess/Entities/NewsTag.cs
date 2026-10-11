namespace FUNews.DataAccess.Entities;

public class NewsTag
{
    public string NewsArticleID { get; set; } = null!;

    public int TagID { get; set; }

    // Navigation properties
    public virtual NewsArticle NewsArticle { get; set; } = null!;

    public virtual Tag Tag { get; set; } = null!;
}
