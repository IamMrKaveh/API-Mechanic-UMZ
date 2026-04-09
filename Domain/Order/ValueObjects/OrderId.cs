namespace Domain.Order.ValueObjects;

public sealed record OrderId
{
    public Guid Value { get; }

    private OrderId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("OrderId cannot be empty.", nameof(value));

        Value = value;
    }

    public static OrderId NewId() => new(Guid.NewGuid());

    public static OrderId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}