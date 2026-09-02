namespace Infrastructure.Search;

public class ElasticsearchOutboxMessage
{
    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = null!;
    public Guid EntityId { get; private set; }
    public string Document { get; private set; } = null!;
    public string ChangeType { get; private set; } = null!;
    public string IdempotencyKey { get; private set; } = null!;
    public int RetryCount { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? Error { get; private set; }
    public bool IsPoisoned { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? NextAttemptAt { get; private set; }

    private ElasticsearchOutboxMessage()
    { }

    public static ElasticsearchOutboxMessage Create(
        string entityType,
        Guid entityId,
        string document,
        string changeType)
        => Create(entityType, entityId, document, changeType, discriminator: null);

    public static ElasticsearchOutboxMessage Create(
        string entityType,
        Guid entityId,
        string document,
        string changeType,
        string? discriminator)
    {
        var now = DateTime.UtcNow;
        var idempotencyKey = string.IsNullOrWhiteSpace(discriminator)
            ? $"{entityType}:{entityId}:{changeType}"
            : $"{entityType}:{entityId}:{changeType}:{discriminator}";

        return new ElasticsearchOutboxMessage
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Document = document,
            ChangeType = changeType,
            IdempotencyKey = idempotencyKey,
            RetryCount = 0,
            CreatedAt = now,
            NextAttemptAt = now,
            IsPoisoned = false
        };
    }

    public void MarkProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
        Error = null;
        NextAttemptAt = null;
    }

    public void MarkFailed(string error, TimeSpan nextAttemptDelay)
    {
        RetryCount++;
        Error = error;
        NextAttemptAt = DateTime.UtcNow.Add(nextAttemptDelay);
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
}
