using Domain.Security.Events;

namespace Application.Auth.EventHandlers;

public sealed class AllSessionsRevokedEventHandler(ILogger<AllSessionsRevokedEventHandler> logger) : INotificationHandler<AllSessionsRevokedEvent>
{
    public Task Handle(
        AllSessionsRevokedEvent notification,
        CancellationToken ct)
    {
        logger.LogInformation(
            "All sessions revoked for user {UserId}. Count: {Count}, Reason: {Reason}",
            notification.UserId.Value, notification.RevokedCount, notification.Reason);
        return Task.CompletedTask;
    }
}