using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;

namespace Domain.Payment.Events;

public sealed class PaymentInitiatedEvent(PaymentTransactionId paymentTransactionId, OrderId orderId, decimal amount) : DomainEvent
{
    public PaymentTransactionId PaymentTransactionId { get; } = paymentTransactionId;
    public OrderId OrderId { get; } = orderId;
    public decimal Amount { get; } = amount;
}