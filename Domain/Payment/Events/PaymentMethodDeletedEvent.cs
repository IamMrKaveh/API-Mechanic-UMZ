using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Payment.Events;

public sealed record PaymentMethodDeletedEvent(
    PaymentMethodId PaymentMethodId,
    UserId? DeletedBy) : DomainEvent;
