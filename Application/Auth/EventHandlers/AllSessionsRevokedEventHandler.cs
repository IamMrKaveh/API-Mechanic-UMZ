using System.Diagnostics;
using Domain.Security.Events;
using Microsoft.Extensions.Logging;

namespace Application.Auth.EventHandlers;

public sealed class AllSessionsRevokedEventHandler(
    IAuditService auditService,
    ILogger<AllSessionsRevokedEventHandler> logger)
    : INotificationHandler<DomainEventNotification<AllSessionsRevokedEvent>>
{
    public async Task Handle(
        DomainEventNotification<AllSessionsRevokedEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;
        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = domainEvent.UserId,
            ["EventName"] = nameof(AllSessionsRevokedEvent),
            ["RevokedCount"] = domainEvent.RevokedCount,
            ["Reason"] = domainEvent.Reason.ToString()
        }))
        {
            logger.LogInformation(
                "All sessions revoked for user {UserId}. Count: {RevokedCount}, Reason: {Reason}",
                domainEvent.UserId,
                domainEvent.RevokedCount,
                domainEvent.Reason);

            await auditService.LogSystemEventAsync(
                "All Session Revoked",
                $"All sessions revoked for user {domainEvent.UserId}. Count: {domainEvent.RevokedCount}, Reason: {domainEvent.Reason}",
                ct);
        }
    }
}