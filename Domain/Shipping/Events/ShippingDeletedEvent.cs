using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Events;

public sealed class ShippingDeletedEvent : DomainEvent
{
    public ShippingId ShippingId { get; }
    public int? DeletedBy { get; }

    public ShippingDeletedEvent(ShippingId shippingId, int? deletedBy)
    {
        ShippingId = shippingId;
        DeletedBy = deletedBy;
    }
}