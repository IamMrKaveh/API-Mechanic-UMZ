namespace Application.Auth.Features.EventHandlers;

public class UserCreatedEventHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedEventHandler> _logger;

    public UserCreatedEventHandler(
        ILogger<UserCreatedEventHandler> logger
        )
    {
        _logger = logger;
    }

    public Task Handle(
        UserCreatedEvent notification,
        CancellationToken ct
        )
    {
        _logger.LogInformation(
            "Domain Event: New user created. UserId={UserId}, PhoneNumber={PhoneNumber}.",
            notification.UserId, notification.PhoneNumber);

        return Task.CompletedTask;
    }
}