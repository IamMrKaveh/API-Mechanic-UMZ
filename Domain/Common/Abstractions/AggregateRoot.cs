namespace Domain.Common.Abstractions;

public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public int Version { get; protected set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot()
    { }

    protected AggregateRoot(TId id) : base(id)
    {
    }

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
        Version++;
    }

    protected void IncrementVersion() => Version++;

    public void ClearDomainEvents() => _domainEvents.Clear();
}
