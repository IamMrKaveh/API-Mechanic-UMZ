namespace Domain.Discount.Entities;

public sealed class DiscountRestriction : Entity<DiscountRestrictionId>
{
    private DiscountRestriction()
    { }

    public DiscountCodeId DiscountCodeId { get; private set; } = default!;
    public DiscountRestrictionType RestrictionType { get; private set; }
    public string RestrictionValue { get; private set; } = default!;

    internal static DiscountRestriction Create(
        DiscountRestrictionId id,
        DiscountCodeId discountCodeId,
        DiscountRestrictionType restrictionType,
        string restrictionValue)
    {
        return new DiscountRestriction
        {
            Id = id,
            DiscountCodeId = discountCodeId,
            RestrictionType = restrictionType,
            RestrictionValue = restrictionValue
        };
    }
}