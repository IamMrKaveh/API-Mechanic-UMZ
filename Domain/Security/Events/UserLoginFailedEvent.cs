using Domain.User.ValueObjects;

namespace Domain.Security.Events;

public sealed record UserLoginFailedEvent(
    UserId UserId,
    int FailedAttempts) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}