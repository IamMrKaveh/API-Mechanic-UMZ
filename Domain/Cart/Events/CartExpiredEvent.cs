using Domain.Cart.ValueObjects;

namespace Domain.Cart.Events;

public class CartExpiredEvent(CartId cartId, GuestToken? guestToken) : DomainEvent
{
    public CartId CartId { get; } = cartId;
    public GuestToken? GuestToken { get; } = guestToken;
}