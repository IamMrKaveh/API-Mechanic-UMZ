namespace Domain.Auth;

public class UserSession : IAuditable
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User.User User { get; set; } = null!;

    public required string TokenSelector { get; set; }
    public required string TokenVerifierHash { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public required string CreatedByIp { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public string? UserAgent { get; set; }

    public string? SessionType { get; set; } = "refresh";

    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
}