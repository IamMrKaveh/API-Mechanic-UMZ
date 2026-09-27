using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;

namespace Domain.Payment.Events;

public sealed record PaymentFailedEvent(PaymentTransactionId PaymentTransactionId, OrderId OrderId, string Reason) : DomainEvent;
