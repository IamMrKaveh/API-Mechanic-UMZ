namespace Domain.User;

public class UserOtp
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public required string OtpHash { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsUsed { get; set; }

    public int AttemptCount { get; set; }

    public DateTime? LockedUntil { get; set; }
}