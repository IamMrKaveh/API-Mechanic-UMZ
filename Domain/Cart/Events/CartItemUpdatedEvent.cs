using Domain.Cart.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Cart.Events;

public class CartItemUpdatedEvent(CartId cartId, VariantId variantId, int oldQuantity, int newQuantity) : DomainEvent
{
    public CartId CartId { get; } = cartId;
    public VariantId VariantId { get; } = variantId;
    public int OldQuantity { get; } = oldQuantity;
    public int NewQuantity { get; } = newQuantity;
}