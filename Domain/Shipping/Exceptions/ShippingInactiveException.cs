using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Exceptions;

public sealed class ShippingInactiveException(ShippingId shippingId)
    : DomainException($"روش ارسال '{shippingId}' غیرفعال است.")
{
    public ShippingId ShippingId { get; } = shippingId;
}