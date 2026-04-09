using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Exceptions;

public sealed class DefaultShippingCannotBeDeletedException : DomainException
{
    public ShippingId ShippingId { get; }

    public override string ErrorCode => "DEFAULT_SHIPPING_CANNOT_BE_DELETED";

    public DefaultShippingCannotBeDeletedException(ShippingId shippingId)
        : base($"امکان حذف روش ارسال پیش‌فرض '{shippingId}' وجود ندارد.")
    {
        ShippingId = shippingId;
    }
}