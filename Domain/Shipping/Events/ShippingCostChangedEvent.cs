using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Events;

public sealed record ShippingCostChangedEvent(ShippingId ShippingId, decimal PreviousCost, decimal NewCost) : DomainEvent;
