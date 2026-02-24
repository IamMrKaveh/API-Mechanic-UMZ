namespace Domain.Cart.Events;

public class CartItemAddedEvent : DomainEvent
{
    public int CartId { get; }
    public int VariantId { get; }
    public int Quantity { get; }

    public CartItemAddedEvent(int cartId, int variantId, int quantity)
    {
        CartId = cartId;
        VariantId = variantId;
        Quantity = quantity;
    }
}