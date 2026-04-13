using Application.Common.Events;
using Domain.Security.Events;

namespace Application.Auth.EventHandlers;

public sealed class AllSessionsRevokedEventHandler(IAuditService auditService)
    : INotificationHandler<DomainEventNotification<AllSessionsRevokedEvent>>
{
    public async Task Handle(
        DomainEventNotification<AllSessionsRevokedEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;
        await auditService.LogSystemEventAsync(
            "All Session Revoked",
            $"All sessions revoked for user {domainEvent.UserId}. Count: {domainEvent.RevokedCount}, Reason: {domainEvent.Reason}",
            ct);
    }
}