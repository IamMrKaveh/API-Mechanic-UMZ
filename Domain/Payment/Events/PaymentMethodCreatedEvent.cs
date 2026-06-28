using Domain.Payment.ValueObjects;

namespace Domain.Payment.Events;

public sealed class PaymentMethodCreatedEvent(
    PaymentMethodId paymentMethodId,
    PaymentMethodName name,
    PaymentMethodCode code) : DomainEvent
{
    public PaymentMethodId PaymentMethodId { get; } = paymentMethodId;
    public PaymentMethodName Name { get; } = name;
    public PaymentMethodCode Code { get; } = code;
}