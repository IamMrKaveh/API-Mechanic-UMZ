using Domain.Security.Events;

namespace Application.Auth.EventHandlers;

public sealed class UserLoggedInEventHandler(ILogger<UserLoggedInEventHandler> logger) : INotificationHandler<UserLoggedInEvent>
{
    public Task Handle(
        UserLoggedInEvent notification,
        CancellationToken ct)
    {
        logger.LogInformation("User {UserId} logged in", notification.UserId.Value);
        return Task.CompletedTask;
    }
}