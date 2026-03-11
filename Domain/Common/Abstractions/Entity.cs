namespace Domain.Common.Abstractions;

public abstract class Entity<TId> where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public TId Id { get; protected init; } = default!;
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected Entity()
    { }

    protected Entity(TId id) => Id = id;

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        var other = (Entity<TId>)obj;
        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        => left is not null && right is not null && left.Equals(right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
        => !(left == right);
}