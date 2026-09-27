using Domain.User.ValueObjects;

namespace Domain.Security.Events;

public sealed record UserLockedOutEvent(
    UserId UserId,
    DateTime LockoutEnd,
    int FailedAttempts) : DomainEvent;
