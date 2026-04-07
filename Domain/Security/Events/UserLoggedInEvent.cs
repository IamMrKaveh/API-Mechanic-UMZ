using Domain.User.ValueObjects;
using Domain.Common.Events;

namespace Domain.Security.Events;

public sealed class UserLoggedInEvent(UserId userId) : DomainEvent
{
    public UserId UserId { get; } = userId;
}