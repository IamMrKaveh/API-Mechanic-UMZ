using Domain.User.ValueObjects;
using Domain.Common.Events;
using Domain.Common.ValueObjects;

namespace Domain.User.Events;

public sealed class UserEmailVerifiedEvent(UserId userId, Email email) : DomainEvent
{
    public UserId UserId { get; } = userId;
    public Email Email { get; } = email;
}