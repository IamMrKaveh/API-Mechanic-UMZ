namespace DataAccessLayer.Models.Auth;

public class TRefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public virtual TUsers? User { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    [MaxLength(45)]
    public string CreatedByIp { get; set; } = string.Empty;
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    [MaxLength(255)]
    public string? UserAgent { get; set; }
}