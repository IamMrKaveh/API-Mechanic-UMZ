using System;

namespace Domain.Discount.ValueObjects;

public sealed record DiscountCodeId
{
    public Guid Value { get; }

    private DiscountCodeId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("DiscountCodeId cannot be empty.", nameof(value));

        Value = value;
    }

    public static DiscountCodeId NewId() => new(Guid.NewGuid());

    public static DiscountCodeId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}