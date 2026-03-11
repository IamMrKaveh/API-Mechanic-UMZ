namespace Domain.Support.ValueObjects;

public sealed record TicketId(Guid Value)
{
    public static TicketId NewId() => new(Guid.NewGuid());
    public static TicketId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}