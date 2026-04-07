using Domain.Cart.ValueObjects;

namespace Domain.Cart.Events;

public class CartClearedEvent(CartId cartId) : DomainEvent
{
    public CartId CartId { get; } = cartId;
}