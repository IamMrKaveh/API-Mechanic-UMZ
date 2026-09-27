using Domain.Payment.ValueObjects;

namespace Domain.Payment.Events;

public sealed record PaymentMethodActivatedEvent(PaymentMethodId PaymentMethodId) : DomainEvent;
