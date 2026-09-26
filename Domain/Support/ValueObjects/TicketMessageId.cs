namespace Domain.Support.ValueObjects;

public sealed record TicketMessageId : StronglyTypedId<TicketMessageId>
{
    private TicketMessageId(Guid value) : base(value) { }

    public static implicit operator Guid(TicketMessageId id) => id.Value;

    public override string ToString() => Value.ToString();
}
