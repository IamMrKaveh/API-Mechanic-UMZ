using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Payment.Events;

public sealed record PaymentRefundedEvent(
    PaymentTransactionId PaymentTransactionId,
    OrderId OrderId,
    UserId UserId,
    Money Amount,
    string? Reason) : DomainEvent;
