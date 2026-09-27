using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Shipping.Events;

public sealed record ShippingDeletedEvent(
    ShippingId ShippingId,
    UserId? DeletedBy) : DomainEvent;
