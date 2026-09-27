using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Events;

public sealed record ShippingSetAsDefaultEvent(ShippingId ShippingId, ShippingName Name) : DomainEvent;
