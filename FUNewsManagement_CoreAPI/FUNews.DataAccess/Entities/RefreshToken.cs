namespace FUNews.DataAccess.Entities;

public class RefreshToken
{
    public long TokenID { get; set; }

    public short? AccountID { get; set; }

    public string? UserEmail { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsRevoked { get; set; }

    public string? ReplacedByToken { get; set; }

    // Navigation property
    public virtual SystemAccount? Account { get; set; }
}
