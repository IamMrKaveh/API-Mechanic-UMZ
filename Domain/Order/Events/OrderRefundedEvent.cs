namespace Domain.Order.Events;

public sealed class OrderRefundedEvent(int orderId, int userId, decimal amount) : DomainEvent
{
    public int OrderId { get; } = orderId;
    public int UserId { get; } = userId;
    public decimal Amount { get; } = amount;
}