using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;

namespace Domain.Payment.Events;

public sealed record PaymentInitiatedEvent(PaymentTransactionId PaymentTransactionId, OrderId OrderId, decimal Amount) : DomainEvent;
