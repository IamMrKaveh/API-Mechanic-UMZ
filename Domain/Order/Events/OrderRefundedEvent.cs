namespace Domain.Order.Events;

public sealed class OrderRefundedEvent : DomainEvent
{
    public int OrderId { get; }
    public int UserId { get; }
    public decimal Amount { get; }

    public OrderRefundedEvent(int orderId, int userId, decimal amount)
    {
        OrderId = orderId;
        UserId = userId;
        Amount = amount;
    }
}