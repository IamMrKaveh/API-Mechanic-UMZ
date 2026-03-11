using Domain.Common.Events;

namespace Domain.Order.Events;

public sealed class OrderPaidEvent : DomainEvent
{
    public Guid OrderId { get; }
    public string OrderNumber { get; }
    public Guid UserId { get; }
    public Guid PaymentTransactionId { get; }
    public decimal PaidAmount { get; }
    public string Currency { get; }

    public OrderPaidEvent(
        Guid orderId,
        string orderNumber,
        Guid userId,
        Guid paymentTransactionId,
        decimal paidAmount,
        string currency)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        UserId = userId;
        PaymentTransactionId = paymentTransactionId;
        PaidAmount = paidAmount;
        Currency = currency;
        EventVersion = 1;
    }
}