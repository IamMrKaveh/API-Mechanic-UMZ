namespace Domain.Order.ValueObjects;

public sealed record OrderStatusId
{
    public Guid Value { get; }

    private OrderStatusId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("OrderStatusId cannot be empty.", nameof(value));

        Value = value;
    }

    public static OrderStatusId NewId() => new(Guid.NewGuid());

    public static OrderStatusId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}