using Domain.Common.Exceptions;
using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Exceptions;

public sealed class ShippingNotAvailableException : DomainException
{
    public ShippingId ShippingId { get; }
    public string Reason { get; }

    public override string ErrorCode => "SHIPPING_NOT_AVAILABLE";

    public ShippingNotAvailableException(ShippingId shippingId, string reason)
        : base($"روش ارسال '{shippingId}' در دسترس نیست. دلیل: {reason}")
    {
        ShippingId = shippingId;
        Reason = reason;
    }
}