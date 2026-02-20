namespace Application.Security.Features.Shared;

public class RateLimit
{
    public int Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public int Count { get; set; }

    public DateTime LastAttempt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }
}