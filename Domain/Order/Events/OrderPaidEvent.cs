using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Order.Events;

public sealed class OrderPaidEvent : DomainEvent
{
    public OrderId OrderId { get; }
    public string OrderNumber { get; }
    public UserId UserId { get; }
    public PaymentTransactionId PaymentTransactionId { get; }
    public decimal PaidAmount { get; }
    public string Currency { get; }

    public OrderPaidEvent(
        OrderId orderId,
        string orderNumber,
        UserId userId,
        PaymentTransactionId paymentTransactionId,
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