namespace MainApi.Services.Notification;

public interface INotificationService
{
    Task CreateNotificationAsync(int userId, string title, string message, string type, string? actionUrl = null, int? relatedEntityId = null, string? relatedEntityType = null);
    Task<IEnumerable<TNotification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false);
    Task<bool> MarkAsReadAsync(int notificationId, int userId);
    Task<int> GetUnreadCountAsync(int userId);
}