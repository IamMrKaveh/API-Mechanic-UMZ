namespace Domain.Variant.ValueObjects;

public sealed record VariantAttributeId(Guid Value)
{
    public static VariantAttributeId NewId() => new(Guid.NewGuid());
    public static VariantAttributeId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}