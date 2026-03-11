namespace Domain.Cart.Events;

public class CartClearedEvent(int cartId) : DomainEvent
{
    public int CartId { get; } = cartId;
}