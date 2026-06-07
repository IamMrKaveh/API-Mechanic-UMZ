namespace Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    public OutboxMessageId Id { get; private set; } = OutboxMessageId.NewId();
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }
    public bool IsPoisoned { get; private set; }

    private OutboxMessage()
    { }

    public static OutboxMessage Create(string type, string payload, DateTime createdAt)
    {
        return new OutboxMessage
        {
            Id = OutboxMessageId.NewId(),
            Type = type,
            Payload = payload,
            CreatedAt = createdAt
        };
    }

    public void MarkProcessed(DateTime processedAt)
    {
        ProcessedAt = processedAt;
        Error = null;
    }

    public void MarkFailed(string error)
    {
        RetryCount++;
        Error = error;
    }

    public void MarkPoisoned(string error)
    {
        IsPoisoned = true;
        Error = error;
    }
}