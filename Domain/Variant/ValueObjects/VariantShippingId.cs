using System;

namespace Domain.Variant.ValueObjects;

public sealed record VariantShippingId
{
    public Guid Value { get; }

    private VariantShippingId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("VariantShippingId cannot be empty.", nameof(value));

        Value = value;
    }

    public static VariantShippingId NewId() => new(Guid.NewGuid());

    public static VariantShippingId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}