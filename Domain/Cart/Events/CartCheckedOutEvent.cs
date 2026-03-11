using Domain.Common.Events;

namespace Domain.Cart.Events;

public sealed class CartCheckedOutEvent : DomainEvent
{
    public Guid CartId { get; }
    public Guid? UserId { get; }
    public int ItemCount { get; }
    public decimal TotalAmount { get; }

    public CartCheckedOutEvent(Guid cartId, Guid? userId, int itemCount, decimal totalAmount)
    {
        CartId = cartId;
        UserId = userId;
        ItemCount = itemCount;
        TotalAmount = totalAmount;
        EventVersion = 1;
    }
}