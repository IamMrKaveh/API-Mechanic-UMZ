namespace Infrastructure.Persistence.Outbox;

public sealed class OutboxArchiveMessage
{
    public OutboxMessageId Id { get; private set; } = OutboxMessageId.NewId();
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime ProcessedAt { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }
    public bool IsPoisoned { get; private set; }
    public string? TraceParent { get; private set; }
    public string? TraceState { get; private set; }
    public DateTime ArchivedAt { get; private set; }

    private OutboxArchiveMessage()
    { }

    public static OutboxArchiveMessage FromProcessed(OutboxMessage source, DateTime archivedAt)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        if (source.ProcessedAt is null)
            throw new InvalidOperationException("Only processed outbox messages can be archived.");

        return new OutboxArchiveMessage
        {
            Id = source.Id,
            Type = source.Type,
            Payload = source.Payload,
            CreatedAt = source.CreatedAt,
            ProcessedAt = source.ProcessedAt.Value,
            Error = source.Error,
            RetryCount = source.RetryCount,
            IsPoisoned = source.IsPoisoned,
            TraceParent = source.TraceParent,
            TraceState = source.TraceState,
            ArchivedAt = archivedAt
        };
    }
}
