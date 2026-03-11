namespace Domain.Variant.ValueObjects;

public sealed record ProductVariantAttributeId(Guid Value)
{
    public static ProductVariantAttributeId NewId() => new(Guid.NewGuid());
    public static ProductVariantAttributeId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}