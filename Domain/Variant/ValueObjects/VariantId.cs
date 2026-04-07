namespace Domain.Variant.ValueObjects;

public sealed record VariantId(Guid Value)
{
    public static VariantId NewId() => new(Guid.NewGuid());
    public static VariantId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}