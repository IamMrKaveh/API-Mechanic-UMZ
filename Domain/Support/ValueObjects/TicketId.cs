namespace Domain.Support.ValueObjects;

public sealed record TicketId
{
    public Guid Value { get; }

    private TicketId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("TicketId cannot be empty.", nameof(value));

        Value = value;
    }

    public static TicketId NewId() => new(Guid.NewGuid());

    public static TicketId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}