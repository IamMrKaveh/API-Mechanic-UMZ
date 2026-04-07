using System;

namespace Domain.Discount.ValueObjects;

public sealed record DiscountRestrictionId
{
    public Guid Value { get; }

    private DiscountRestrictionId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("DiscountRestrictionId cannot be empty.", nameof(value));

        Value = value;
    }

    public static DiscountRestrictionId NewId() => new(Guid.NewGuid());

    public static DiscountRestrictionId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}