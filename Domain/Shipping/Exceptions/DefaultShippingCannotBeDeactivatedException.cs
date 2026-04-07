using Domain.Common.Exceptions;
using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Exceptions;

public sealed class DefaultShippingCannotBeDeactivatedException : DomainException
{
    public ShippingId ShippingId { get; }

    public override string ErrorCode => "DEFAULT_SHIPPING_CANNOT_BE_DEACTIVATED";

    public DefaultShippingCannotBeDeactivatedException(ShippingId shippingId)
        : base($"امکان غیرفعال کردن روش ارسال پیش‌فرض '{shippingId}' وجود ندارد.")
    {
        ShippingId = shippingId;
    }
}