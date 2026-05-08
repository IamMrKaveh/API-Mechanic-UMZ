using Domain.Variant.ValueObjects;

namespace Infrastructure.Variant.Converters;

internal sealed class VariantShippingIdConverter : StronglyTypedIdConverter<VariantShippingId>
{
    public VariantShippingIdConverter() : base(VariantShippingId.From)
    {
    }
}