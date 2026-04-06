using Application.Cache.Contracts;
using Domain.User.Events;

namespace Application.Auth.EventHandlers;

public sealed class UserDeactivatedEventHandler(
    ICacheInvalidationService cacheInvalidation,
    ILogger<UserDeactivatedEventHandler> logger) : INotificationHandler<UserDeactivatedEvent>
{
    public async Task Handle(
        UserDeactivatedEvent notification,
        CancellationToken ct)
    {
        await cacheInvalidation.InvalidateUserCacheAsync(notification.UserId.Value, ct);
        logger.LogInformation("User {UserId} deactivated", notification.UserId.Value);
    }
}