using Domain.Common.Abstractions;
using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Shipping.Events;

public sealed record ShippingDeletedEvent(
    ShippingId ShippingId,
    UserId? DeletedBy) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}