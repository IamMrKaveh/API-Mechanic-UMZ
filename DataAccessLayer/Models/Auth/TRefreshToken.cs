namespace DataAccessLayer.Models.Auth;

public class TRefreshToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public virtual TUsers? User { get; set; }


    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByIp { get; set; } = string.Empty;
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? UserAgent { get; set; }
}

