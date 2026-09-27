using Domain.Payment.ValueObjects;

namespace Domain.Payment.Events;

public sealed record PaymentMethodDeactivatedEvent(PaymentMethodId PaymentMethodId) : DomainEvent;
