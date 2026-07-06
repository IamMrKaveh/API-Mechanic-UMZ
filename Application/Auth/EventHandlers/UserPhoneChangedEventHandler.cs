using System.Diagnostics;
using Domain.User.Events;
using Microsoft.Extensions.Logging;

namespace Application.Auth.EventHandlers;

public sealed class UserPhoneChangedEventHandler(
    ICacheInvalidationService cacheInvalidation,
    IAuditService auditService,
    ILogger<UserPhoneChangedEventHandler> logger)
    : INotificationHandler<DomainEventNotification<UserPhoneChangedEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserPhoneChangedEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;
        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = domainEvent.UserId,
            ["EventName"] = nameof(UserPhoneChangedEvent)
        }))
        {
            await cacheInvalidation.InvalidateUserCacheAsync(domainEvent.UserId, ct);

            logger.LogInformation(
                "Phone changed for user {UserId} from {OldPhone} to {NewPhone}",
                domainEvent.UserId,
                domainEvent.OldPhone,
                domainEvent.NewPhone);

            await auditService.LogSystemEventAsync(
                "User phone changed",
                $"Phone changed for user {domainEvent.UserId} from {domainEvent.OldPhone} to {domainEvent.NewPhone}",
                ct);
        }
    }
}