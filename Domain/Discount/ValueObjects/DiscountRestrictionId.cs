namespace Domain.Discount.ValueObjects;

public sealed record DiscountRestrictionId(Guid Value)
{
    public static DiscountRestrictionId NewId() => new(Guid.NewGuid());
    public static DiscountRestrictionId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}