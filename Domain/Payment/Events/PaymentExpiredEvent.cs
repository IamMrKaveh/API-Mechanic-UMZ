using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;

namespace Domain.Payment.Events;

public sealed class PaymentExpiredEvent(PaymentTransactionId paymentTransactionId, OrderId orderId, decimal amount, string authority) : DomainEvent
{
    public PaymentTransactionId PaymentTransactionId { get; } = paymentTransactionId;
    public OrderId OrderId { get; } = orderId;
    public decimal Amount { get; } = amount;
    public string Authority { get; } = authority;
}