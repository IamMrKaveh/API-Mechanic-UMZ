namespace Domain.Support.ValueObjects;

public sealed record TicketId : StronglyTypedId<TicketId>
{
    private TicketId(Guid value) : base(value) { }

    public static implicit operator Guid(TicketId id) => id.Value;

    public override string ToString() => Value.ToString();
}
