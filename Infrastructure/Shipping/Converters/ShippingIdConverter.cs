using Domain.Shipping.ValueObjects;

namespace Infrastructure.Shipping.Converters;

internal sealed class ShippingIdConverter : StronglyTypedIdConverter<ShippingId>
{
    public ShippingIdConverter() : base(ShippingId.From)
    {
    }
}