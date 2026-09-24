namespace FUNews.DataAccess.Entities;

public class NewsArticle
{
    public string NewsArticleID { get; set; } = null!;

    public string? NewsTitle { get; set; }

    public string Headline { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public string? NewsContent { get; set; }

    public string? NewsSource { get; set; }

    public short? CategoryID { get; set; }

    public bool? NewsStatus { get; set; }

    public short? CreatedByID { get; set; }

    public short? UpdatedByID { get; set; }

    public DateTime? ModifiedDate { get; set; }

    // Navigation properties
    public virtual Category? Category { get; set; }

    public virtual SystemAccount? CreatedBy { get; set; }

    public virtual SystemAccount? UpdatedBy { get; set; }

    public virtual ICollection<NewsTag> NewsTags { get; set; } = new List<NewsTag>();
}
