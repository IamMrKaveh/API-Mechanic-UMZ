using Domain.Payment.ValueObjects;

namespace Domain.Payment.Events;

public sealed record PaymentMethodUpdatedEvent(
    PaymentMethodId PaymentMethodId,
    PaymentMethodName Name) : DomainEvent;
