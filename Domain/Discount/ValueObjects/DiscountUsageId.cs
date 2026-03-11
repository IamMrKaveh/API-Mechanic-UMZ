namespace Domain.Discount.ValueObjects;

public sealed record DiscountUsageId(Guid Value)
{
    public static DiscountUsageId NewId() => new(Guid.NewGuid());
    public static DiscountUsageId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}