using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Events;

public sealed record ShippingUpdatedEvent(ShippingId ShippingId, ShippingName Name) : DomainEvent;
