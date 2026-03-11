using Domain.Common.Events;

namespace Domain.Cart.Events;

public sealed class CartItemRemovedEvent : DomainEvent
{
    public Guid CartId { get; }
    public Guid VariantId { get; }
    public int RemovedQuantity { get; }

    public CartItemRemovedEvent(Guid cartId, Guid variantId, int removedQuantity)
    {
        CartId = cartId;
        VariantId = variantId;
        RemovedQuantity = removedQuantity;
        EventVersion = 1;
    }
}