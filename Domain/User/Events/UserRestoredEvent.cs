using Domain.User.ValueObjects;

namespace Domain.User.Events;

public sealed class UserRestoredEvent(UserId userId) : DomainEvent
{
    public UserId UserId { get; } = userId;
}