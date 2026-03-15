using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed record UserDefaultAddressChangedEvent(
    UserId UserId,
    UserAddressId? PreviousDefaultAddressId,
    UserAddressId NewDefaultAddressId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}