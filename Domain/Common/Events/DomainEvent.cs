namespace Domain.Common.Events;

public abstract class DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public Guid CorrelationId { get; private set; } = Guid.NewGuid();
    public Guid? CausationId { get; private set; }
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
}