namespace Application.Auth.Features.EventHandlers;

public class UserLoggedInEventHandler : INotificationHandler<UserLoggedInEvent>
{
    private readonly ILogger<UserLoggedInEventHandler> _logger;

    public UserLoggedInEventHandler(ILogger<UserLoggedInEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(UserLoggedInEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Domain Event: User {UserId} logged in.", notification.UserId);
        // می‌توان اینجا Side Effect‌ها مانند ارسال نوتیفیکیشن یا آپدیت آمار را انجام داد
        return Task.CompletedTask;
    }
}