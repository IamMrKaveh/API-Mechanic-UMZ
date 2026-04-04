using Domain.User.Events;

namespace Application.Auth.EventHandlers;

public class UserCreatedEventHandler(ILogger<UserCreatedEventHandler> logger) : INotificationHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedEventHandler> _logger = logger;

    public Task Handle(
        UserCreatedEvent notification,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Domain Event: New user created. UserId={UserId}, PhoneNumber={PhoneNumber}.",
            notification.UserId, notification.PhoneNumber);

        return Task.CompletedTask;
    }
}