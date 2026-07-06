using System.Diagnostics;
using Domain.User.Events;
using Microsoft.Extensions.Logging;

namespace Application.Auth.EventHandlers;

public sealed class UserDeactivatedEventHandler(
    ICacheInvalidationService cacheInvalidation,
    IAuditService auditService,
    ILogger<UserDeactivatedEventHandler> logger)
    : INotificationHandler<DomainEventNotification<UserDeactivatedEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserDeactivatedEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;
        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = domainEvent.UserId,
            ["EventName"] = nameof(UserDeactivatedEvent)
        }))
        {
            await cacheInvalidation.InvalidateUserCacheAsync(domainEvent.UserId, ct);

            logger.LogInformation("User {UserId} deactivated", domainEvent.UserId);

            await auditService.LogSystemEventAsync(
                "Deactive User",
                $"User {domainEvent.UserId} deactivated",
                ct);
        }
    }
}