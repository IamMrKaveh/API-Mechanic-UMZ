namespace Domain.Notification.Services;

/// <summary>
/// Domain Service برای عملیات‌های پیچیده اعلان
/// Stateless - بدون وابستگی به Infrastructure
/// </summary>
public sealed class NotificationDomainService
{
    /// <summary>
    /// اعتبارسنجی دسترسی کاربر به اعلان
    /// </summary>
    public (bool HasAccess, string? Error) ValidateUserAccess(Notification notification, int userId)
    {
        Guard.Against.Null(notification, nameof(notification));

        if (notification.UserId != userId)
        {
            return (false, "شما دسترسی به این اعلان را ندارید.");
        }

        return (true, null);
    }

    /// <summary>
    /// علامت‌گذاری چند اعلان به عنوان خوانده شده
    /// </summary>
    public int MarkMultipleAsRead(IEnumerable<Notification> notifications)
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

    /// <summary>
    /// فیلتر اعلان‌های قابل حذف
    /// </summary>
    public IEnumerable<Notification> FilterDeletableNotifications(
        IEnumerable<Notification> notifications,
        TimeSpan minAge)
    {
        Guard.Against.Null(notifications, nameof(notifications));

        return notifications.Where(n => n.IsOlderThan(minAge) && n.IsRead);
    }

    /// <summary>
    /// محاسبه آمار اعلان‌های کاربر
    /// </summary>
    public NotificationStatistics CalculateStatistics(IEnumerable<Notification> notifications)
    {
        Guard.Against.Null(notifications, nameof(notifications));

        var notificationList = notifications.ToList();

        var total = notificationList.Count;
        var unread = notificationList.Count(n => !n.IsRead);
        var read = total - unread;

        var typeBreakdown = notificationList
            .GroupBy(n => n.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        return new NotificationStatistics(total, read, unread, typeBreakdown);
    }

    /// <summary>
    /// گروه‌بندی اعلان‌ها بر اساس تاریخ
    /// </summary>
    public Dictionary<string, List<Notification>> GroupByDate(IEnumerable<Notification> notifications)
    {
        Guard.Against.Null(notifications, nameof(notifications));

        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);
        var lastWeek = today.AddDays(-7);

        return notifications
            .GroupBy(n =>
            {
                var date = n.CreatedAt.Date;
                if (date == today) return "امروز";
                if (date == yesterday) return "دیروز";
                if (date >= lastWeek) return "هفته گذشته";
                return "قبل‌تر";
            })
            .ToDictionary(g => g.Key, g => g.OrderByDescending(n => n.CreatedAt).ToList());
    }
}

/// <summary>
/// آمار اعلان‌ها
/// </summary>
public sealed record NotificationStatistics(
    int TotalCount,
    int ReadCount,
    int UnreadCount,
    Dictionary<string, int> TypeBreakdown)
{
    public decimal ReadPercentage =>
        TotalCount > 0 ? Math.Round((decimal)ReadCount / TotalCount * 100, 2) : 0;

    public decimal UnreadPercentage =>
        TotalCount > 0 ? Math.Round((decimal)UnreadCount / TotalCount * 100, 2) : 0;

    public bool HasUnread => UnreadCount > 0;
}