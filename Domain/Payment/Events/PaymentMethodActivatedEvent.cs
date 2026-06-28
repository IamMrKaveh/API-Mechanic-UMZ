using Domain.Payment.ValueObjects;

namespace Domain.Payment.Events;

public sealed class PaymentMethodActivatedEvent(PaymentMethodId paymentMethodId) : DomainEvent
{
    public PaymentMethodId PaymentMethodId { get; } = paymentMethodId;
}