using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;

namespace Domain.Payment.Events;

public sealed record PaymentExpiredEvent(PaymentTransactionId PaymentTransactionId, OrderId OrderId, decimal Amount, string Authority) : DomainEvent;
