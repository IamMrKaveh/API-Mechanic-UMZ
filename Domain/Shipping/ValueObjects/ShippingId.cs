namespace Domain.Shipping.ValueObjects;

public sealed record ShippingId(Guid Value)
{
    public static ShippingId NewId() => new(Guid.NewGuid());
    public static ShippingId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}