namespace Domain.Cart.Events;

public class CartItemUpdatedEvent : DomainEvent
{
    public int CartId { get; }
    public int VariantId { get; }
    public int OldQuantity { get; }
    public int NewQuantity { get; }

    public CartItemUpdatedEvent(int cartId, int variantId, int oldQuantity, int newQuantity)
    {
        CartId = cartId;
        VariantId = variantId;
        OldQuantity = oldQuantity;
        NewQuantity = newQuantity;
    }
}