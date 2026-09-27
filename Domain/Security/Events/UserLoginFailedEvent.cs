using Domain.User.ValueObjects;

namespace Domain.Security.Events;

public sealed record UserLoginFailedEvent(
    UserId UserId,
    int FailedAttempts) : DomainEvent;
