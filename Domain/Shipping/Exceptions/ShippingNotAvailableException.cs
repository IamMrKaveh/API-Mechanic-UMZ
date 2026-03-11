namespace Domain.Shipping.Exceptions;

public sealed class ShippingNotAvailableException(ShippingId shippingId, string reason) : DomainException($"روش ارسال '{shippingId}' در دسترس نیست. دلیل: {reason}")
{
    public ShippingId ShippingId { get; } = shippingId;
    public string Reason { get; } = reason;
}