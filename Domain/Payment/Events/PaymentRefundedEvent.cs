using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Payment.Events;

public sealed class PaymentRefundedEvent(
    PaymentTransactionId paymentTransactionId,
    OrderId orderId,
    UserId userId,
    decimal amount,
    string? reason) : DomainEvent
{
    public PaymentTransactionId PaymentTransactionId { get; } = paymentTransactionId;
    public OrderId OrderId { get; } = orderId;
    public UserId UserId { get; } = userId;
    public decimal Amount { get; } = amount;
    public string? Reason { get; } = reason;
}