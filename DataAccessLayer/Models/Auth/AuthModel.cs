namespace DataAccessLayer.Models.Auth;

[Index(nameof(TokenHash), IsUnique = true)]
[Index(nameof(UserId), nameof(ExpiresAt))]
[Index(nameof(CreatedByIp), nameof(CreatedAt))]
public class TUserSession : IAuditable
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public TUsers User { get; set; } = null!;

    [Required, MaxLength(512)]
    public required string TokenHash { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [Required, MaxLength(45)]
    public required string CreatedByIp { get; set; }

    public DateTime? RevokedAt { get; set; }

    [MaxLength(512)]
    public string? ReplacedByTokenHash { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(50)]
    public string? SessionType { get; set; } = "refresh";

    [NotMapped]
    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
}