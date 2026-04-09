namespace Domain.Order.ValueObjects;

public sealed record OrderItemId
{
    public Guid Value { get; }

    private OrderItemId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("OrderItemId cannot be empty.", nameof(value));

        Value = value;
    }

    public static OrderItemId NewId() => new(Guid.NewGuid());

    public static OrderItemId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}