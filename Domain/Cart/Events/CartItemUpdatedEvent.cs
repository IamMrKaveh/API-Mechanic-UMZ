namespace Domain.Cart.Events;

public class CartItemUpdatedEvent(int cartId, int variantId, int oldQuantity, int newQuantity) : DomainEvent
{
    public int CartId { get; } = cartId;
    public int VariantId { get; } = variantId;
    public int OldQuantity { get; } = oldQuantity;
    public int NewQuantity { get; } = newQuantity;
}