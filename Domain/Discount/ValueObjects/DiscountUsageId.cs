namespace Domain.Discount.ValueObjects;

public sealed record DiscountUsageId
{
    public Guid Value { get; }

    private DiscountUsageId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("DiscountUsageId cannot be empty.", nameof(value));

        Value = value;
    }

    public static DiscountUsageId NewId() => new(Guid.NewGuid());

    public static DiscountUsageId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}