using Domain.Payment.ValueObjects;

namespace Domain.Payment.Events;

public sealed class PaymentMethodUpdatedEvent(
    PaymentMethodId paymentMethodId,
    PaymentMethodName name) : DomainEvent
{
    public PaymentMethodId PaymentMethodId { get; } = paymentMethodId;
    public PaymentMethodName Name { get; } = name;
}