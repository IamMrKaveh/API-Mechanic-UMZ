using Domain.Common.Events;

namespace Domain.Cart.Events;

public sealed class CartCreatedEvent : DomainEvent
{
    public Guid CartId { get; }
    public Guid? UserId { get; }
    public string? GuestToken { get; }

    public CartCreatedEvent(Guid cartId, Guid? userId, string? guestToken)
    {
        CartId = cartId;
        UserId = userId;
        GuestToken = guestToken;
        EventVersion = 1;
    }
}