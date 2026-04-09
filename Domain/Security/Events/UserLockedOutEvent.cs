using Domain.User.ValueObjects;

namespace Domain.Security.Events;

public sealed class UserLockedOutEvent(
    UserId userId,
    DateTime lockoutEnd,
    int failedAttempts) : DomainEvent
{
    public UserId UserId { get; } = userId;
    public DateTime LockoutEnd { get; } = lockoutEnd;
    public int FailedAttempts { get; } = failedAttempts;
}