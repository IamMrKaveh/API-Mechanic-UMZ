namespace Domain.Security;

public class RateLimitEntry : BaseEntity
{
    public string Key { get; private set; } = string.Empty;
    public int Count { get; private set; }
    public DateTime LastAttempt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private RateLimitEntry()
    { }

    public static RateLimitEntry Create(string key, int count, DateTime lastAttempt, DateTime expiresAt)
    {
        return new RateLimitEntry
        {
            Key = key,
            Count = count,
            LastAttempt = lastAttempt,
            ExpiresAt = expiresAt
        };
    }

    public void Update(int count, DateTime lastAttempt, DateTime expiresAt)
    {
        Count = count;
        LastAttempt = lastAttempt;
        ExpiresAt = expiresAt;
    }
}