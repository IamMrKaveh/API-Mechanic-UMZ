using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Order.Events;

public sealed record OrderPaidEvent(
    OrderId OrderId,
    OrderNumber OrderNumber,
    UserId UserId,
    PaymentTransactionId PaymentTransactionId,
    decimal PaidAmount,
    string Currency) : DomainEvent;
