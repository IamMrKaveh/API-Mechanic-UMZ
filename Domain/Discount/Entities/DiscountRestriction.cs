using Domain.Discount.Aggregates;
using Domain.Discount.Enums;
using Domain.Discount.ValueObjects;

namespace Domain.Discount.Entities;

public sealed class DiscountRestriction : Entity<DiscountRestrictionId>
{
    private DiscountRestriction()
    { }

    public DiscountCodeId DiscountCodeId { get; private set; } = default!;
    public DiscountCode DiscountCode { get; private set; } = default!;

    public DiscountRestrictionType RestrictionType { get; private set; }
    public string RestrictionValue { get; private set; } = default!;
}