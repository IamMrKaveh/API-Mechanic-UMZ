namespace Domain.Common;

/// <summary>
/// Persisted domain event waiting to be published by the outbox worker.
/// Written inside the same database transaction as the aggregate change.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; private set; }

    /// <summary>Full type name of the original domain event.</summary>
    public string Type { get; private set; } = null!;

    /// <summary>JSON-serialized event payload.</summary>
    public string Payload { get; private set; } = null!;

    public DateTime OccurredAt { get; private set; }

    /// <summary>Set by the outbox worker once the event has been published.</summary>
    public DateTime? ProcessedAt { get; private set; }

    public string? Error { get; private set; }

    private OutboxMessage()
    { }

    public static OutboxMessage From(DomainEvent domainEvent)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = domainEvent.GetType().FullName!,
            Payload = System.Text.Json.JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            OccurredAt = DateTime.UtcNow
        };
    }

    public void MarkProcessed() => ProcessedAt = DateTime.UtcNow;

    public void MarkFailed(string error)
    {
        Error = error;
        ProcessedAt = DateTime.UtcNow;
    }
}