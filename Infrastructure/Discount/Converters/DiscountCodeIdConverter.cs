using Domain.Discount.ValueObjects;

namespace Infrastructure.Discount.Converters;

internal sealed class DiscountCodeIdConverter : StronglyTypedIdConverter<DiscountCodeId>
{
    public DiscountCodeIdConverter() : base(DiscountCodeId.From)
    {
    }
}