using Application.Common.Events;
using Domain.User.Events;

namespace Application.Auth.EventHandlers;

public sealed class UserPhoneChangedEventHandler(
    ICacheInvalidationService cacheInvalidation,
    IAuditService auditService)
    : INotificationHandler<DomainEventNotification<UserPhoneChangedEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserPhoneChangedEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;
        await cacheInvalidation.InvalidateUserCacheAsync(domainEvent.UserId, ct);
        await auditService.LogSystemEventAsync(
            "User phone changed",
            $"Phone changed for user {domainEvent.UserId} from {domainEvent.OldPhone} to {domainEvent.NewPhone}",
            ct);
    }
}