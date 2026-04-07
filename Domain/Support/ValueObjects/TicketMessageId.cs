using System;

namespace Domain.Support.ValueObjects;

public sealed record TicketMessageId
{
    public Guid Value { get; }

    private TicketMessageId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("TicketMessageId cannot be empty.", nameof(value));

        Value = value;
    }

    public static TicketMessageId NewId() => new(Guid.NewGuid());

    public static TicketMessageId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}