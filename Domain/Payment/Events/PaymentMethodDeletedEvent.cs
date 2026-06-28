using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Payment.Events;

public sealed class PaymentMethodDeletedEvent(
    PaymentMethodId paymentMethodId,
    UserId? deletedBy) : DomainEvent
{
    public PaymentMethodId PaymentMethodId { get; } = paymentMethodId;
    public UserId? DeletedBy { get; } = deletedBy;
}