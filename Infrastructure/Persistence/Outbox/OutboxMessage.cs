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
}