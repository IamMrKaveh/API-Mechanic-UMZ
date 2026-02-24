namespace Domain.Cart.Events;

public class CartClearedEvent : DomainEvent
{
    public int CartId { get; }

    public CartClearedEvent(int cartId)
    {
        CartId = cartId;
    }
}