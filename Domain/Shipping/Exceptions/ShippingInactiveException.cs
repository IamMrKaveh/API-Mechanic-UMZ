using Domain.Common.Exceptions;
using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Exceptions;

public sealed class ShippingInactiveException : DomainException
{
    public ShippingId ShippingId { get; }

    public override string ErrorCode => "SHIPPING_INACTIVE";

    public ShippingInactiveException(ShippingId shippingId)
        : base($"روش ارسال '{shippingId}' غیرفعال است.")
    {
        ShippingId = shippingId;
    }
}