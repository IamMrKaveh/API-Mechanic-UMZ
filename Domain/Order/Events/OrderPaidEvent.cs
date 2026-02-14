namespace Domain.Order.Events;

public sealed class OrderPaidEvent : DomainEvent
{
    public int OrderId { get; }
    public int UserId { get; }
    public decimal Amount { get; }
    public long? RefId { get; }

    public OrderPaidEvent(int orderId, int userId, decimal amount, long? refId = null)
    {
        OrderId = orderId;
        UserId = userId;
        Amount = amount;
        RefId = refId;
    }
}