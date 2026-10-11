namespace FUNews.DataAccess.Entities;

public class TagLearning
{
    public int TagID { get; set; }

    public int SelectionCount { get; set; }

    public DateTime LastSelectedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual Tag Tag { get; set; } = null!;
}
