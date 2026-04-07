using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Events;

public sealed class ShippingUpdatedEvent(ShippingId shippingId, ShippingName name) : DomainEvent
{
    public ShippingId ShippingId { get; } = shippingId;
    public ShippingName Name { get; } = name;
}