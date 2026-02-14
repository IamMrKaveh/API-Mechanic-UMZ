namespace Application.Notification.Contracts;

/// <summary>
/// Repository Interface برای Notification Aggregate
/// فقط عملیات‌های ضروری برای Persistence - بدون Business Logic
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// دریافت نوتیفیکیشن بر اساس شناسه
    /// </summary>
    Task<Domain.Notification.Notification?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// دریافت اعلان‌های کاربر
    /// </summary>
    Task<(IEnumerable<Domain.Notification.Notification> Items, int TotalCount)> GetByUserIdAsync(
        int userId,
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// دریافت اعلان‌های خوانده نشده کاربر
    /// </summary>
    Task<IEnumerable<Domain.Notification.Notification>> GetUnreadByUserIdAsync(
        int userId,
        int? limit = null,
        CancellationToken ct = default);

    /// <summary>
    /// شمارش اعلان‌های خوانده نشده کاربر
    /// </summary>
    Task<int> CountUnreadByUserIdAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// دریافت اعلان‌های مرتبط با موجودیت
    /// </summary>
    Task<IEnumerable<Domain.Notification.Notification>> GetByRelatedEntityAsync(
        string entityType,
        int entityId,
        CancellationToken ct = default);

    /// <summary>
    /// دریافت اعلان‌های اخیر کاربر
    /// </summary>
    Task<IEnumerable<Domain.Notification.Notification>> GetRecentByUserIdAsync(
        int userId,
        int count,
        CancellationToken ct = default);

    /// <summary>
    /// علامت‌گذاری همه به عنوان خوانده شده
    /// </summary>
    Task<int> MarkAllAsReadByUserIdAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// حذف اعلان‌های قدیمی
    /// </summary>
    Task<int> DeleteOldNotificationsAsync(DateTime olderThan, CancellationToken ct = default);

    /// <summary>
    /// افزودن نوتیفیکیشن جدید
    /// </summary>
    Task AddAsync(Domain.Notification.Notification notification, CancellationToken ct = default);

    /// <summary>
    /// به‌روزرسانی اعلان
    /// </summary>
    void Update(Domain.Notification.Notification notification);
}