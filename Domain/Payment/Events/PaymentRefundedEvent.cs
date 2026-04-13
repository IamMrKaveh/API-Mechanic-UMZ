using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Payment.Events;

public sealed class PaymentRefundedEvent(
    PaymentTransactionId paymentTransactionId,
    OrderId orderId,
    UserId userId,
    Money amount,
    string? reason) : DomainEvent
{
    public PaymentTransactionId PaymentTransactionId { get; } = paymentTransactionId;
    public OrderId OrderId { get; } = orderId;
    public UserId UserId { get; } = userId;
    public Money Amount { get; } = amount;
    public string? Reason { get; } = reason;
}