using Domain.User.ValueObjects;

namespace Domain.Security.Events;

public sealed record UserLoggedInEvent(UserId UserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}