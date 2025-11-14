namespace Domain.Security;

public class RateLimit
{
    public int Id { get; set; }

    public required string Key { get; set; }

    public int Count { get; set; }

    public DateTime LastAttempt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }
}