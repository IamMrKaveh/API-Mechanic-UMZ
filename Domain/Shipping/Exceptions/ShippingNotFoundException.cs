using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Exceptions;

public sealed class ShippingNotFoundException(ShippingId shippingId) : DomainException($"روش ارسال با شناسه '{shippingId}' یافت نشد.")
{
    public ShippingId ShippingId { get; } = shippingId;
}