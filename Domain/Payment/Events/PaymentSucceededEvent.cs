using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;

namespace Domain.Payment.Events;

public sealed class PaymentSucceededEvent(
    PaymentTransactionId paymentTransactionId,
    OrderId orderId,
    long refId,
    int userId = 0,
    decimal amount = 0) : DomainEvent
{
    public PaymentTransactionId PaymentTransactionId { get; } = paymentTransactionId;
    public OrderId OrderId { get; } = orderId;
    public long RefId { get; } = refId;
    public int UserId { get; } = userId;
    public decimal Amount { get; } = amount;
}