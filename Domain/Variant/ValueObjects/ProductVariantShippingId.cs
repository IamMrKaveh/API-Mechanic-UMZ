namespace Domain.Variant.ValueObjects;

public sealed record ProductVariantShippingId(Guid Value)
{
    public static ProductVariantShippingId NewId() => new(Guid.NewGuid());
    public static ProductVariantShippingId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}