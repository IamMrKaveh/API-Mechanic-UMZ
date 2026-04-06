using Domain.Security.Events;

namespace Application.Auth.EventHandlers;

public sealed class UserLockedOutEventHandler(ILogger<UserLockedOutEventHandler> logger) : INotificationHandler<UserLockedOutEvent>
{
    public Task Handle(
        UserLockedOutEvent notification,
        CancellationToken ct)
    {
        logger.LogWarning(
            "User {UserId} locked out after {FailedAttempts} failed attempts. Lockout ends at {LockoutEnd}",
            notification.UserId.Value, notification.FailedAttempts, notification.LockoutEnd);
        return Task.CompletedTask;
    }
}