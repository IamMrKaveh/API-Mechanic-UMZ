namespace Domain.Order.ValueObjects;

public sealed record OrderItemId(Guid Value)
{
    public static OrderItemId NewId() => new(Guid.NewGuid());
    public static OrderItemId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}