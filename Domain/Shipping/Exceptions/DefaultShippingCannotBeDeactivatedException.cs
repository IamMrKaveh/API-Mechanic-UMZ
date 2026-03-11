namespace Domain.Shipping.Exceptions;

public sealed class DefaultShippingCannotBeDeactivatedException(ShippingId shippingId)
    : DomainException($"امکان غیرفعال کردن روش ارسال پیش‌فرض '{shippingId}' وجود ندارد.")
{
    public ShippingId ShippingId { get; } = shippingId;
}