using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed record UserRegisteredEvent(
    UserId UserId,
    Email Email,
    string FirstName,
    string LastName) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}