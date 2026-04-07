using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Cart.Events;

public sealed class CartCheckedOutEvent(CartId cartId, UserId? userId, int itemCount, decimal totalAmount) : DomainEvent
{
    public CartId CartId { get; } = cartId;
    public UserId? UserId { get; } = userId;
    public int ItemCount { get; } = itemCount;
    public decimal TotalAmount { get; } = totalAmount;
}