namespace Domain.Variant.ValueObjects;

public sealed record ProductVariantId(Guid Value)
{
    public static ProductVariantId NewId() => new(Guid.NewGuid());
    public static ProductVariantId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}