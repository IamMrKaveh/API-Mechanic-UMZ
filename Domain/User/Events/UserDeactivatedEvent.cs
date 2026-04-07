using Domain.User.ValueObjects;
using Domain.Common.Events;

namespace Domain.User.Events;

public sealed class UserDeactivatedEvent(UserId userId) : DomainEvent
{
    public UserId UserId { get; } = userId;
}