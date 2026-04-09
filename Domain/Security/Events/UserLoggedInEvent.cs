using Domain.User.ValueObjects;

namespace Domain.Security.Events;

public sealed class UserLoggedInEvent(UserId userId) : DomainEvent
{
    public UserId UserId { get; } = userId;
}