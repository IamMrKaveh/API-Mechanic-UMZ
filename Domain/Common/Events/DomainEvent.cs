using System.Text.Json.Serialization;

namespace Domain.Common.Events;

public abstract record DomainEvent : IDomainEvent
{
    [JsonInclude]
    public Guid EventId { get; private set; } = Guid.NewGuid();

    [JsonInclude]
    public DateTime OccurredAt { get; private set; } = DateTime.UtcNow;

    [JsonInclude]
    public Guid CorrelationId { get; private set; } = Guid.NewGuid();

    [JsonInclude]
    public Guid? CausationId { get; private set; }

    [JsonInclude]
    public int EventVersion { get; protected init; } = 1;

    protected DomainEvent()
    { }

    protected DomainEvent(Guid correlationId)
    {
        CorrelationId = correlationId;
    }

    public DomainEvent WithCorrelationId(Guid correlationId)
    {
        CorrelationId = correlationId;
        return this;
    }

    public DomainEvent WithCausationId(Guid causationId)
    {
        CausationId = causationId;
        return this;
    }

    public void SetCorrelation(Guid correlationId) => CorrelationId = correlationId;

    public void SetCausation(Guid causationId) => CausationId = causationId;
}
