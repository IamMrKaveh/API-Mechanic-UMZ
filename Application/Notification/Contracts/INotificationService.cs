namespace Application.Notification.Contracts;

/// <summary>
/// سرویس ایجاد و مدیریت نوتیفیکیشن‌ها
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// ایجاد نوتیفیکیشن
    /// </summary>
    Task CreateNotificationAsync(
        int userId,
        string title,
        string message,
        string type,
        string? actionUrl = null,
        int? relatedEntityId = null,
        string? relatedEntityType = null,
        CancellationToken ct = default);

    /// <summary>
    /// ارسال نوتیفیکیشن تغییر وضعیت سفارش
    /// </summary>
    Task SendOrderStatusNotificationAsync(
        int userId,
        int orderId,
        string oldStatus,
        string newStatus,
        CancellationToken ct = default);

    /// <summary>
    /// ارسال نوتیفیکیشن پرداخت
    /// </summary>
    Task SendPaymentNotificationAsync(
        int userId,
        int orderId,
        bool isSuccess,
        string? refId = null,
        CancellationToken ct = default);

    /// <summary>
    /// ارسال نوتیفیکیشن موجودی کم
    /// </summary>
    Task SendLowStockNotificationAsync(
        int productId,
        string productName,
        int currentStock,
        CancellationToken ct = default);
}