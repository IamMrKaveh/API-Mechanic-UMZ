namespace Application.Auth.Features.EventHandlers;

public class UserLockedOutEventHandler : INotificationHandler<UserLockedOutEvent>
{
    private readonly IAuditService _auditService;
    private readonly ILogger<UserLockedOutEventHandler> _logger;

    public UserLockedOutEventHandler(
        IAuditService auditService,
        ILogger<UserLockedOutEventHandler> logger
        )
    {
        _auditService = auditService;
        _logger = logger;
    }

    public async Task Handle(
        UserLockedOutEvent notification,
        CancellationToken ct
        )
    {
        _logger.LogWarning(
            "Domain Event: User {UserId} locked out until {LockoutEnd}.",
            notification.UserId, notification.LockoutEndTime);

        await _auditService.LogSecurityEventAsync(
            "AccountLockedOut",
            $"حساب کاربر {notification.UserId} به دلیل تلاش‌های ناموفق قفل شد تا {notification.LockoutEndTime:yyyy-MM-dd HH:mm}.",
            "system",
            notification.UserId);
    }
}