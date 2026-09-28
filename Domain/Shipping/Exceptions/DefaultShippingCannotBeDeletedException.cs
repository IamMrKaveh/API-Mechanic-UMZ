using Domain.Shipping.ValueObjects;
using SharedKernel.Exceptions;

namespace Domain.Shipping.Exceptions;

public sealed class DefaultShippingCannotBeDeletedException(ShippingId shippingId)
    : ConflictException<ShippingId>(
        "DEFAULT_SHIPPING_CANNOT_BE_DELETED",
        shippingId,
        $"امکان حذف روش ارسال پیش‌فرض '{shippingId}' وجود ندارد.")
{
    public ShippingId ShippingId => Value;
}
