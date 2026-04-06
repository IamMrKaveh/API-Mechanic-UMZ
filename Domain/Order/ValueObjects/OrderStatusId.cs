namespace Domain.Order.ValueObjects;

public sealed record OrderStatusId(Guid Value)
{
    public static OrderStatusId NewId() => new(Guid.NewGuid());
    public static OrderStatusId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}