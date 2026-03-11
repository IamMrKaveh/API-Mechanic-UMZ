namespace Application.Auth.EventHandlers;

public class UserLockedOutEventHandler(
    IAuditService auditService,
    ILogger<UserLockedOutEventHandler> logger) : INotificationHandler<UserLockedOutEvent>
{
    private readonly IAuditService _auditService = auditService;
    private readonly ILogger<UserLockedOutEventHandler> _logger = logger;

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