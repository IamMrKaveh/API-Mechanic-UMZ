using System;

namespace Domain.Variant.ValueObjects;

public sealed record VariantId
{
    public Guid Value { get; }

    private VariantId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("VariantId cannot be empty.", nameof(value));

        Value = value;
    }

    public static VariantId NewId() => new(Guid.NewGuid());

    public static VariantId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}