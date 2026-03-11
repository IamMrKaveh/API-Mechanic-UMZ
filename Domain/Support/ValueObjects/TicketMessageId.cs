namespace Domain.Support.ValueObjects;

public sealed record TicketMessageId(Guid Value)
{
    public static TicketMessageId NewId() => new(Guid.NewGuid());
    public static TicketMessageId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}