namespace Domain.Cart.Events;

public class CartExpiredEvent(int cartId, string? guestToken) : DomainEvent
{
    public int CartId { get; } = cartId;
    public string? GuestToken { get; } = guestToken;
}