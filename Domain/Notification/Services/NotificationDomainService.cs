using Domain.User.ValueObjects;

namespace Domain.Notification.Services;

public sealed class NotificationDomainService
{
    public static Result ValidateUserAccess(
        Aggregates.Notification notification,
        UserId userId)
    {
        Guard.Against.Null(notification, nameof(notification));
        Guard.Against.Null(userId, nameof(userId));

        if (notification.UserId.Value != userId.Value)
        {
            return Result.Failure(
                Error.Forbidden(
                    "Notification.AccessDenied",
                    "شما دسترسی به این اعلان را ندارید."));
        }

        return Result.Success();
    }

    public static int MarkMultipleAsRead(
        IEnumerable<Aggregates.Notification> notifications)
    {
        Guard.Against.Null(notifications, nameof(notifications));

        var count = 0;

        foreach (var notification in notifications)
        {
            if (!notification.IsRead)
            {
                notification.MarkAsRead();
                count++;
            }
        }

        return count;
    }

    public static IReadOnlyList<Aggregates.Notification> FilterDeletableNotifications(
        IEnumerable<Aggregates.Notification> notifications,
        TimeSpan minAge,
        DateTime now)
    {
        Guard.Against.Null(notifications, nameof(notifications));

        return notifications
            .Where(n => n.IsRead && (now - n.CreatedAt) >= minAge)
            .ToList()
            .AsReadOnly();
    }

    public static ValueObjects.NotificationPriority DeterminePriority(
        ValueObjects.NotificationType type)
    {
        Guard.Against.Null(type, nameof(type));

        if (type.IsHighPriority())
            return ValueObjects.NotificationPriority.High;

        if (type.IsOrderRelated())
            return ValueObjects.NotificationPriority.Normal;

        return ValueObjects.NotificationPriority.Low;
    }
}