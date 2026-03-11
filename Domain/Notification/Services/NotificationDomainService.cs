namespace Domain.Notification.Services;

public sealed class NotificationDomainService
{
    public static Result ValidateUserAccess(Notification notification, int userId)
    {
        Guard.Against.Null(notification, nameof(notification));

        if (notification.UserId != userId)
            return Result.Failure("شما دسترسی به این اعلان را ندارید.");

        return Result.Success();
    }

    public static int MarkMultipleAsRead(IEnumerable<Notification> notifications)
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

    public static IReadOnlyList<Notification> FilterDeletableNotifications(
        IEnumerable<Notification> notifications,
        TimeSpan minAge)
    {
        Guard.Against.Null(notifications, nameof(notifications));

        return notifications
            .Where(n => n.IsOlderThan(minAge) && n.IsRead)
            .ToList()
            .AsReadOnly();
    }

    public static ValueObjects.NotificationPriority DeterminePriority(ValueObjects.NotificationType type)
    {
        Guard.Against.Null(type, nameof(type));

        if (type.IsHighPriority())
            return ValueObjects.NotificationPriority.High;

        if (type.IsOrderRelated())
            return ValueObjects.NotificationPriority.Normal;

        return ValueObjects.NotificationPriority.Low;
    }
}