using Domain.User.ValueObjects;
using Domain.Common.Events;

namespace Domain.Security.Events;

public sealed class UserLoginFailedEvent(
    UserId userId,
    int failedAttempts) : DomainEvent
{
    public UserId UserId { get; } = userId;
    public int FailedAttempts { get; } = failedAttempts;
}