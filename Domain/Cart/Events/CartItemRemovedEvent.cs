using Domain.Cart.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Cart.Events;

public sealed class CartItemRemovedEvent(CartId cartId, ProductVariantId variantId, int removedQuantity) : DomainEvent
{
    public CartId CartId { get; } = cartId;
    public ProductVariantId VariantId { get; } = variantId;
    public int RemovedQuantity { get; } = removedQuantity;
}