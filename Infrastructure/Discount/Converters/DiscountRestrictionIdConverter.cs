using Domain.Discount.ValueObjects;

namespace Infrastructure.Discount.Converters;

internal sealed class DiscountRestrictionIdConverter : StronglyTypedIdConverter<DiscountRestrictionId>
{
    public DiscountRestrictionIdConverter() : base(DiscountRestrictionId.From)
    {
    }
}