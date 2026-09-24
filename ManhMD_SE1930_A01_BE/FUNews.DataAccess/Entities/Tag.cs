namespace FUNews.DataAccess.Entities;

public class Tag
{
    public int TagID { get; set; }

    public string? TagName { get; set; }

    public string? Note { get; set; }

    // Navigation properties
    public virtual ICollection<NewsTag> NewsTags { get; set; } = new List<NewsTag>();
}
