namespace Domain.Cart.Events;

public class CartItemRemovedEvent : DomainEvent
{
    public int CartId { get; }
    public int VariantId { get; }

    public CartItemRemovedEvent(int cartId, int variantId)
    {
        CartId = cartId;
        VariantId = variantId;
    }
}