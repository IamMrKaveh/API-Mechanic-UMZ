using Domain.Payment.ValueObjects;

namespace Domain.Payment.Events;

public sealed class PaymentMethodDeactivatedEvent(PaymentMethodId paymentMethodId) : DomainEvent
{
    public PaymentMethodId PaymentMethodId { get; } = paymentMethodId;
}