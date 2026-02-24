namespace Application.Auth.Features.EventHandlers;

public class UserPhoneChangedEventHandler : INotificationHandler<UserPhoneChangedEvent>
{
    private readonly ISessionService _sessionManager;
    private readonly IAuditService _auditService;
    private readonly ILogger<UserPhoneChangedEventHandler> _logger;

    public UserPhoneChangedEventHandler(
        ISessionService sessionManager,
        IAuditService auditService,
        ILogger<UserPhoneChangedEventHandler> logger
        )
    {
        _sessionManager = sessionManager;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task Handle(
        UserPhoneChangedEvent notification,
        CancellationToken ct
        )
    {
        _logger.LogInformation(
            "Domain Event: User {UserId} phone changed from {OldPhone} to {NewPhone}.",
            notification.UserId, notification.OldPhoneNumber, notification.NewPhoneNumber);

        await _sessionManager.RevokeAllUserSessionsAsync(notification.UserId, ct);

        await _auditService.LogSecurityEventAsync(
            "PhoneNumberChanged",
            $"شماره تلفن کاربر {notification.UserId} از {notification.OldPhoneNumber} به {notification.NewPhoneNumber} تغییر کرد.",
            "system",
            notification.UserId);
    }
}