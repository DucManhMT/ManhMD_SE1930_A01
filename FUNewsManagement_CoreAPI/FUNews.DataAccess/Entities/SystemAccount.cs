namespace FUNews.DataAccess.Entities;

public class SystemAccount
{
    public short AccountID { get; set; }

    public string? AccountName { get; set; }

    public string? AccountEmail { get; set; }

    public int? AccountRole { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public string? AccountPassword { get; set; }

    // Navigation properties
    public virtual ICollection<NewsArticle> CreatedNewsArticles { get; set; } = new List<NewsArticle>();

    public virtual ICollection<NewsArticle> UpdatedNewsArticles { get; set; } = new List<NewsArticle>();
}
