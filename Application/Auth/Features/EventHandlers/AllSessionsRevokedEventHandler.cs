namespace Application.Auth.Features.EventHandlers;

public class AllSessionsRevokedEventHandler : INotificationHandler<AllSessionsRevokedEvent>
{
    private readonly ISessionService _sessionManager;
    private readonly ILogger<AllSessionsRevokedEventHandler> _logger;

    public AllSessionsRevokedEventHandler(
        ISessionService sessionManager,
        ILogger<AllSessionsRevokedEventHandler> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public async Task Handle(AllSessionsRevokedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Domain Event: All sessions revoked for user {UserId}.", notification.UserId);
        await _sessionManager.RevokeAllUserSessionsAsync(notification.UserId, cancellationToken);
    }
}