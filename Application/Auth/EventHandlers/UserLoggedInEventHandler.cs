using Application.Common.Events;
using Domain.Security.Events;

namespace Application.Auth.EventHandlers;

public sealed class UserLoggedInEventHandler(IAuditService auditService)
    : INotificationHandler<DomainEventNotification<UserLoggedInEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserLoggedInEvent> notification,
        CancellationToken ct)
    {
        await auditService.LogSystemEventAsync(
            "User login",
            $"User {notification.DomainEvent.UserId} logged in",
            ct);
    }
}