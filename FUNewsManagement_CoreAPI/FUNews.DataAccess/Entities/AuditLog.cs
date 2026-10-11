namespace FUNews.DataAccess.Entities;

public class AuditLog
{
    public long AuditID { get; set; }

    public int? UserID { get; set; }

    public string? UserName { get; set; }

    public string Action { get; set; } = string.Empty;

    public string Entity { get; set; } = string.Empty;

    public string? EntityID { get; set; }

    public string? BeforeJSON { get; set; }

    public string? AfterJSON { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
