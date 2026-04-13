using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Payment.Events;

public sealed class PaymentSucceededEvent(
    PaymentTransactionId paymentTransactionId,
    OrderId orderId,
    long refId,
    UserId userId,
    Money amount) : DomainEvent
{
    public PaymentTransactionId PaymentTransactionId { get; } = paymentTransactionId;
    public OrderId OrderId { get; } = orderId;
    public long RefId { get; } = refId;
    public UserId UserId { get; } = userId;
    public Money Amount { get; } = amount;
}