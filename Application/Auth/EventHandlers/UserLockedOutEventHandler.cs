using System.Diagnostics;
using Domain.Security.Events;
using Microsoft.Extensions.Logging;

namespace Application.Auth.EventHandlers;

public sealed class UserLockedOutEventHandler(
    IAuditService auditService,
    ILogger<UserLockedOutEventHandler> logger)
    : INotificationHandler<DomainEventNotification<UserLockedOutEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserLockedOutEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;
        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = domainEvent.UserId,
            ["EventName"] = nameof(UserLockedOutEvent),
            ["FailedAttempts"] = domainEvent.FailedAttempts,
            ["LockoutEnd"] = domainEvent.LockoutEnd
        }))
        {
            logger.LogWarning(
                "User {UserId} locked out after {FailedAttempts} failed attempts until {LockoutEnd}",
                domainEvent.UserId,
                domainEvent.FailedAttempts,
                domainEvent.LockoutEnd);

            await auditService.LogSystemEventAsync(
                "User Locked out",
                $"User {domainEvent.UserId} locked out after {domainEvent.FailedAttempts} failed attempts. Lockout ends at {domainEvent.LockoutEnd}",
                ct);
        }
    }
}