using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Events;

public sealed record ShippingCreatedEvent(ShippingId ShippingId, ShippingName Name, decimal BaseCost) : DomainEvent;
