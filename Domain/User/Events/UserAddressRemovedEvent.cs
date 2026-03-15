using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed record UserAddressRemovedEvent(
    UserId UserId,
    UserAddressId AddressId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}