using Domain.Payment.ValueObjects;

namespace Domain.Payment.Events;

public sealed record PaymentMethodCreatedEvent(
    PaymentMethodId PaymentMethodId,
    PaymentMethodName Name,
    PaymentMethodCode Code) : DomainEvent;
