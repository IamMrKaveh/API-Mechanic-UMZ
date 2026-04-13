using Application.Common.Events;
using Domain.Security.Events;

namespace Application.Auth.EventHandlers;

public sealed class UserLockedOutEventHandler(IAuditService auditService)
    : INotificationHandler<DomainEventNotification<UserLockedOutEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserLockedOutEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;
        await auditService.LogSystemEventAsync(
            "User Locked out",
            $"User {domainEvent.UserId} locked out after {domainEvent.FailedAttempts} failed attempts. Lockout ends at {domainEvent.LockoutEnd}",
            ct);
    }
}