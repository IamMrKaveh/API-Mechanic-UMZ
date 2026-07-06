using System.Diagnostics;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.User.Events;
using Microsoft.Extensions.Logging;

namespace Application.Auth.EventHandlers;

public sealed class UserPasswordChangedEventHandler(
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ILogger<UserPasswordChangedEventHandler> logger)
    : INotificationHandler<DomainEventNotification<UserPasswordChangedEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserPasswordChangedEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;
        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = domainEvent.UserId,
            ["EventName"] = nameof(UserPasswordChangedEvent)
        }))
        {
            try
            {
                await sessionRepository.RevokeAllByUserIdAsync(
                    domainEvent.UserId,
                    SessionRevocationReason.PasswordChanged,
                    ct);

                await unitOfWork.SaveChangesAsync(ct);

                logger.LogInformation(
                    "All sessions revoked for user {UserId} due to password change",
                    domainEvent.UserId);

                await auditService.LogSecurityEventAsync(
                    "PasswordChanged",
                    $"تمام جلسات کاربر {domainEvent.UserId} به دلیل تغییر گذرواژه لغو شد.",
                    IpAddress.Unknown,
                    domainEvent.UserId,
                    ct);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to revoke sessions after password change for user {UserId}",
                    domainEvent.UserId);

                await auditService.LogSystemEventAsync(
                    "PasswordChangedSessionRevocationFailed",
                    $"Failed to revoke sessions after password change for user {domainEvent.UserId}: {ex.Message}",
                    ct);
            }
        }
    }
}