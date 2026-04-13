using Application.Common.Events;
using Domain.User.Events;

namespace Application.Auth.EventHandlers;

public sealed class UserDeactivatedEventHandler(
    ICacheInvalidationService cacheInvalidation,
    IAuditService auditService)
    : INotificationHandler<DomainEventNotification<UserDeactivatedEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserDeactivatedEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;
        await cacheInvalidation.InvalidateUserCacheAsync(domainEvent.UserId, ct);
        await auditService.LogSystemEventAsync(
            "Deactive User",
            $"User {domainEvent.UserId} deactivated",
            ct);
    }
}