namespace Domain.Cart.Events;

public class CartExpiredEvent : DomainEvent
{
    public int CartId { get; }
    public string? GuestToken { get; }

    public CartExpiredEvent(int cartId, string? guestToken)
    {
        CartId = cartId;
        GuestToken = guestToken;
    }
}