namespace Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    private const int MaxPayloadBytes = 64 * 1024;

    public OutboxMessageId Id { get; private set; } = OutboxMessageId.NewId();
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }
    public bool IsPoisoned { get; private set; }
    public string? TraceParent { get; private set; }
    public string? TraceState { get; private set; }

    private OutboxMessage()
    { }

    public static OutboxMessage Create(
        string type,
        string payload,
        DateTime createdAt,
        string? traceParent = null,
        string? traceState = null)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Outbox message type must be provided.", nameof(type));

        if (payload is null)
            throw new ArgumentNullException(nameof(payload));

        var payloadBytes = Encoding.UTF8.GetByteCount(payload);
        if (payloadBytes > MaxPayloadBytes)
            throw new InvalidOperationException(
                $"Outbox payload size ({payloadBytes} bytes) exceeds the maximum allowed size of {MaxPayloadBytes} bytes.");

        return new OutboxMessage
        {
            Id = OutboxMessageId.NewId(),
            Type = type,
            Payload = payload,
            CreatedAt = createdAt,
            TraceParent = traceParent,
            TraceState = traceState
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
