namespace Domain.Support.ValueObjects;

public sealed record TicketId : IStronglyTypedId
{
    public Guid Value { get; }

    private TicketId(Guid value) => Value = value;

    public static TicketId NewId() => new(Guid.NewGuid());

    public static TicketId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("TicketId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(TicketId id) => id.Value;
}