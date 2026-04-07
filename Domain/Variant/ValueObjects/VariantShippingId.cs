namespace Domain.Variant.ValueObjects;

public sealed record VariantShippingId(Guid Value)
{
    public static VariantShippingId NewId() => new(Guid.NewGuid());
    public static VariantShippingId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}