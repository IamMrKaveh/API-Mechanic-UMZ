using System.Diagnostics;
using Domain.Security.Events;
using Microsoft.Extensions.Logging;

namespace Application.Auth.EventHandlers;

public sealed class UserLoggedInEventHandler(
    IAuditService auditService,
    ILogger<UserLoggedInEventHandler> logger)
    : INotificationHandler<DomainEventNotification<UserLoggedInEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserLoggedInEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;
        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = domainEvent.UserId,
            ["EventName"] = nameof(UserLoggedInEvent)
        }))
        {
            logger.LogInformation("User {UserId} logged in", domainEvent.UserId);

            await auditService.LogSystemEventAsync(
                "User login",
                $"User {domainEvent.UserId} logged in",
                ct);
        }
    }
}