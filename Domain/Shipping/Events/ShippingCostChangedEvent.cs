using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Events;

public sealed class ShippingCostChangedEvent(ShippingId shippingId, decimal previousCost, decimal newCost) : DomainEvent
{
    public ShippingId ShippingId { get; } = shippingId;
    public decimal PreviousCost { get; } = previousCost;
    public decimal NewCost { get; } = newCost;
}