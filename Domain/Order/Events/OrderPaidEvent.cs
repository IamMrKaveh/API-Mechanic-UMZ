using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Order.Events;

public sealed class OrderPaidEvent(
    OrderId orderId,
    OrderNumber orderNumber,
    UserId userId,
    PaymentTransactionId paymentTransactionId,
    decimal paidAmount,
    string currency) : DomainEvent
{
    public OrderId OrderId { get; } = orderId;
    public OrderNumber OrderNumber { get; } = orderNumber;
    public UserId UserId { get; } = userId;
    public PaymentTransactionId PaymentTransactionId { get; } = paymentTransactionId;
    public decimal PaidAmount { get; } = paidAmount;
    public string Currency { get; } = currency;
}