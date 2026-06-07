namespace Infrastructure.Search;

public sealed class ElasticsearchOutboxMessage
{
    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string Document { get; private set; } = string.Empty;
    public string ChangeType { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime? NextAttemptAt { get; private set; }
    public string? Error { get; private set; }
    public bool IsPoisoned { get; private set; }

    private ElasticsearchOutboxMessage()
    { }

    public static ElasticsearchOutboxMessage Create(
        string entityType,
        Guid entityId,
        string document,
        string changeType)
        => new()
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Document = document,
            ChangeType = changeType,
            IdempotencyKey = BuildIdempotencyKey(entityType, entityId, changeType),
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
            NextAttemptAt = DateTime.UtcNow
        };

    public void MarkProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
        Error = null;
        NextAttemptAt = null;
    }

    public void MarkFailed(string error, TimeSpan retryDelay)
    {
        RetryCount++;
        Error = error;
        NextAttemptAt = DateTime.UtcNow.Add(retryDelay);
    }

    public void MarkPoisoned(string error)
    {
        IsPoisoned = true;
        Error = error;
        NextAttemptAt = null;
    }

    public void IncrementRetry(string? error = null)
    {
        RetryCount++;
        Error = error;
    }

    private static string BuildIdempotencyKey(string entityType, Guid entityId, string changeType)
        => $"{entityType}:{entityId}:{changeType}";
}