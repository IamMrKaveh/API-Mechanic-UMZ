using Domain.Common.Abstractions;

namespace Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage : AggregateRoot<OutboxMessageId>
{
    public string Type { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }

    private OutboxMessage()
    { }

    public static OutboxMessage Create(string type, string content)
        => new()
        {
            Type = type,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

    public void MarkProcessed() => ProcessedAt = DateTime.UtcNow;

    public void MarkFailed(string error)
    {
        Error = error;
        RetryCount++;
    }
}