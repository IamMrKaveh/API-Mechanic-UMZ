using Application.Cache.Contracts;
using Domain.User.Events;

namespace Application.Auth.EventHandlers;

public sealed class UserPhoneChangedEventHandler(
    ICacheInvalidationService cacheInvalidation,
    ILogger<UserPhoneChangedEventHandler> logger) : INotificationHandler<UserPhoneChangedEvent>
{
    public async Task Handle(
        UserPhoneChangedEvent notification,
        CancellationToken ct)
    {
        await cacheInvalidation.InvalidateUserCacheAsync(notification.UserId.Value, ct);
        logger.LogInformation(
            "Phone changed for user {UserId} from {OldPhone} to {NewPhone}",
            notification.UserId.Value, notification.OldPhone.Value, notification.NewPhone.Value);
    }
}