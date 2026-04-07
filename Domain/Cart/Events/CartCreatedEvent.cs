using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Cart.Events;

public sealed class CartCreatedEvent(CartId cartId, UserId? userId, GuestToken? guestToken) : DomainEvent
{
    public CartId CartId { get; } = cartId;
    public UserId? UserId { get; } = userId;
    public GuestToken? GuestToken { get; } = guestToken;
}