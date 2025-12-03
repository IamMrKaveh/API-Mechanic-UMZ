namespace Application.Common.Interfaces;

public interface INotificationService
{
    Task CreateNotificationAsync(int userId, string title, string message, string type, string? actionUrl = null, int? relatedEntityId = null, string? relatedEntityType = null);
    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId, bool unreadOnly = false, int page = 1, int pageSize = 20);
    Task<bool> MarkAsReadAsync(int notificationId, int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkAllAsReadAsync(int userId);
    Task DeleteNotificationAsync(int notificationId, int userId);
    Task SendOrderStatusNotificationAsync(int userId, int orderId, string oldStatus, string newStatus);
    Task SendPaymentNotificationAsync(int userId, int orderId, bool isSuccess, string? refId = null);
    Task SendLowStockNotificationAsync(int productId, string productName, int currentStock);
}