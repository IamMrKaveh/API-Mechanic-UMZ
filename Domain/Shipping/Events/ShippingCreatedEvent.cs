using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Events;

public sealed class ShippingCreatedEvent(ShippingId shippingId, ShippingName name, decimal baseCost) : DomainEvent
{
    public ShippingId ShippingId { get; } = shippingId;
    public ShippingName Name { get; } = name;
    public decimal BaseCost { get; } = baseCost;
}