using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;

namespace Domain.Payment.Events;

public sealed class PaymentFailedEvent(PaymentTransactionId paymentTransactionId, OrderId orderId, string reason) : DomainEvent
{
    public PaymentTransactionId PaymentTransactionId { get; } = paymentTransactionId;
    public OrderId OrderId { get; } = orderId;
    public string Reason { get; } = reason;
}