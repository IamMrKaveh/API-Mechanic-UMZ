using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed record UserEmailVerifiedEvent(UserId UserId, string Email) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}