namespace Domain.Order.Events;

/// <summary>
/// رویداد انقضای سفارش - زمانی که سفارش در مهلت مقرر پرداخت نشده است.
/// </summary>
public sealed class OrderExpiredEvent : DomainEvent
{
    public int OrderId { get; }
    public int UserId { get; }
    public string OrderNumber { get; }
    public DateTime ExpiredAt { get; }

    public OrderExpiredEvent(int orderId, int userId, string orderNumber)
    {
        OrderId = orderId;
        UserId = userId;
        OrderNumber = orderNumber;
        ExpiredAt = DateTime.UtcNow;
    }
}