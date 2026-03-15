using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Events;

public sealed class ShippingDeactivatedEvent : DomainEvent
{
    public ShippingId ShippingId { get; }
    public string Name { get; }

    public ShippingDeactivatedEvent(ShippingId shippingId, string name)
    {
        ShippingId = shippingId;
        Name = name;
    }
}