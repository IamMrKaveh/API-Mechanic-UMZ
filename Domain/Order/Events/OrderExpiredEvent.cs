namespace Domain.Order.Events;

/// <summary>
/// رویداد انقضای سفارش - زمانی که سفارش در مهلت مقرر پرداخت نشده است.
/// </summary>
public sealed class OrderExpiredEvent(int orderId, int userId, string orderNumber) : DomainEvent
{
    public int OrderId { get; } = orderId;
    public int UserId { get; } = userId;
    public string OrderNumber { get; } = orderNumber;
    public DateTime ExpiredAt { get; } = DateTime.UtcNow;
}