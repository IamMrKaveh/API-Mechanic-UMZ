namespace Infrastructure.Search;

public sealed class ElasticsearchOutboxMessage
{
    public Guid Id { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string Document { get; private set; } = string.Empty;
    public string ChangeType { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? Error { get; private set; }

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
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        };

    public void MarkProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
    }

    public void IncrementRetry(string? error = null)
    {
        RetryCount++;
        Error = error;
    }
}