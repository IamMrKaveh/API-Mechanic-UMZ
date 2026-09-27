using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Payment.Events;

public sealed record PaymentSucceededEvent(
    PaymentTransactionId PaymentTransactionId,
    OrderId OrderId,
    long RefId,
    UserId UserId,
    Money Amount) : DomainEvent;
