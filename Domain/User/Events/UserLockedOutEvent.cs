using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed record UserLockedOutEvent(
    UserId UserId,
    DateTime LockoutEnd) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}