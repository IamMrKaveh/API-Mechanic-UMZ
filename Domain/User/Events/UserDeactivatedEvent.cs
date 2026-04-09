using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed class UserDeactivatedEvent(UserId userId) : DomainEvent
{
    public UserId UserId { get; } = userId;
}