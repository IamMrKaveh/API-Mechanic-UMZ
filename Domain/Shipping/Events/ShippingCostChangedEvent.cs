using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Events;

public sealed class ShippingCostChangedEvent : DomainEvent
{
    public ShippingId ShippingId { get; }
    public decimal PreviousCost { get; }
    public decimal NewCost { get; }

    public ShippingCostChangedEvent(ShippingId shippingId, decimal previousCost, decimal newCost)
    {
        ShippingId = shippingId;
        PreviousCost = previousCost;
        NewCost = newCost;
    }
}