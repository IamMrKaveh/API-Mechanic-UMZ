namespace Domain.Variant.ValueObjects;

public sealed record VariantAttributeId
{
    public Guid Value { get; }

    private VariantAttributeId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("VariantAttributeId cannot be empty.", nameof(value));

        Value = value;
    }

    public static VariantAttributeId NewId() => new(Guid.NewGuid());

    public static VariantAttributeId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}