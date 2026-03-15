using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Events;

public sealed class ShippingUpdatedEvent(ShippingId shippingId, string name) : DomainEvent
{
    public ShippingId ShippingId { get; } = shippingId;
    public string Name { get; } = name;
}