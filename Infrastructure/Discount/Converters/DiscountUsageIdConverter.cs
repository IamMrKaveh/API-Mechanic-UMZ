using Domain.Discount.ValueObjects;

namespace Infrastructure.Discount.Converters;

internal sealed class DiscountUsageIdConverter : StronglyTypedIdConverter<DiscountUsageId>
{
    public DiscountUsageIdConverter() : base(DiscountUsageId.From)
    {
    }
}