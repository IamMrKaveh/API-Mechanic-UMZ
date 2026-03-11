namespace Domain.Discount.ValueObjects;

public sealed record DiscountCodeId(Guid Value)
{
    public static DiscountCodeId NewId() => new(Guid.NewGuid());
    public static DiscountCodeId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}