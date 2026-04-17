namespace Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    public OutboxMessageId Id { get; set; } = OutboxMessageId.NewId();
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }

    private OutboxMessage()
    { }

    public static OutboxMessage Create(string type, string payload)
    {
        return new OutboxMessage
        {
            Id = OutboxMessageId.NewId(),
            Type = type,
            Payload = payload,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
        Error = null;
    }

    public void MarkFailed(string error)
    {
        RetryCount++;
        Error = error;
    }
}