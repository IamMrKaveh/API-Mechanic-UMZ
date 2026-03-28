using Domain.Security.Events;

namespace Application.Auth.EventHandlers;

public class UserLoggedInEventHandler(
    ILogger<UserLoggedInEventHandler> logger) : INotificationHandler<UserLoggedInEvent>
{
    private readonly ILogger<UserLoggedInEventHandler> _logger = logger;

    public Task Handle(
        UserLoggedInEvent notification,
        CancellationToken ct
        )
    {
        _logger.LogInformation("Domain Event: User {UserId} logged in.", notification.UserId);
        return Task.CompletedTask;
    }
}