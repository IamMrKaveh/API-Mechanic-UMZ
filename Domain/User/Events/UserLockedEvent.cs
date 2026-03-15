using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed record UserLockedEvent(
    UserId UserId,
    DateTime LockoutEnd,
    int FailedAttempts) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}