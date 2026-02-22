namespace Application.Auth.Features.EventHandlers;

public class UserDeactivatedEventHandler : INotificationHandler<UserDeactivatedEvent>
{
    private readonly ISessionService _sessionManager;
    private readonly IAuditService _auditService;
    private readonly ILogger<UserDeactivatedEventHandler> _logger;

    public UserDeactivatedEventHandler(
        ISessionService sessionManager,
        IAuditService auditService,
        ILogger<UserDeactivatedEventHandler> logger
        )
    {
        _sessionManager = sessionManager;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task Handle(
        UserDeactivatedEvent notification,
        CancellationToken ct
        )
    {
        _logger.LogInformation("Domain Event: User {UserId} deactivated.", notification.UserId);

        // ابطال تمام سشن‌ها در Infrastructure
        await _sessionManager.RevokeAllUserSessionsAsync(notification.UserId, ct);

        await _auditService.LogSecurityEventAsync(
            "AccountDeactivated",
            $"حساب کاربر {notification.UserId} غیرفعال شد و تمام نشست‌ها ابطال شدند.",
            "system",
            notification.UserId);
    }
}