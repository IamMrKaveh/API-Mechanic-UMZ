namespace Domain.Shipping.Exceptions;

public sealed class DefaultShippingCannotBeDeletedException(ShippingId shippingId)
    : DomainException($"امکان حذف روش ارسال پیش‌فرض '{shippingId}' وجود ندارد.")
{
    public ShippingId ShippingId { get; } = shippingId;
}