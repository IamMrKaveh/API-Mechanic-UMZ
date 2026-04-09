using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Exceptions;

public sealed class ShippingNotFoundException : DomainException
{
    public ShippingId ShippingId { get; }

    public override string ErrorCode => "SHIPPING_NOT_FOUND";

    public ShippingNotFoundException(ShippingId shippingId)
        : base($"روش ارسال با شناسه '{shippingId}' یافت نشد.")
    {
        ShippingId = shippingId;
    }
}