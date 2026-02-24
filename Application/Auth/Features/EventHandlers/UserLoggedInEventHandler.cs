namespace Application.Auth.Features.EventHandlers;

public class UserLoggedInEventHandler : INotificationHandler<UserLoggedInEvent>
{
    private readonly ILogger<UserLoggedInEventHandler> _logger;

    public UserLoggedInEventHandler(
        ILogger<UserLoggedInEventHandler> logger
        )
    {
        _logger = logger;
    }

    public Task Handle(
        UserLoggedInEvent notification,
        CancellationToken ct
        )
    {
        _logger.LogInformation("Domain Event: User {UserId} logged in.", notification.UserId);
        return Task.CompletedTask;
    }
}