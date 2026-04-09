using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Shipping.Events;

public sealed class ShippingDeletedEvent(
    ShippingId shippingId,
    UserId? deletedBy) : DomainEvent
{
    public ShippingId ShippingId { get; } = shippingId;
    public UserId? DeletedBy { get; } = deletedBy;
}