namespace Domain.Support.ValueObjects;

public sealed record TicketMessageId : IStronglyTypedId
{
    public Guid Value { get; }

    private TicketMessageId(Guid value) => Value = value;

    public static TicketMessageId NewId() => new(Guid.NewGuid());

    public static TicketMessageId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("TicketMessageId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(TicketMessageId id) => id.Value;
}