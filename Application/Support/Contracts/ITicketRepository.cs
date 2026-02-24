namespace Application.Support.Contracts;

/// <summary>
/// Repository Interface برای Ticket Aggregate
/// فقط عملیات‌های ضروری برای Persistence - بدون Business Logic
/// </summary>
public interface ITicketRepository
{
    /// <summary>
    /// دریافت تیکت به همراه پیام‌ها
    /// </summary>
    Task<Ticket?> GetByIdWithMessagesAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// دریافت تیکت‌های کاربر
    /// </summary>
    Task<(IEnumerable<Ticket> Items, int TotalCount)> GetByUserIdAsync(
        int userId,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// دریافت تیکت‌های باز
    /// </summary>
    Task<IEnumerable<Ticket>> GetOpenTicketsAsync(CancellationToken ct = default);

    /// <summary>
    /// دریافت تیکت‌های در انتظار پاسخ
    /// </summary>
    Task<IEnumerable<Ticket>> GetAwaitingReplyAsync(CancellationToken ct = default);

    /// <summary>
    /// دریافت تیکت‌های با اولویت بالا
    /// </summary>
    Task<IEnumerable<Ticket>> GetHighPriorityTicketsAsync(CancellationToken ct = default);

    /// <summary>
    /// دریافت لیست صفحه‌بندی شده برای ادمین
    /// </summary>
    Task<(IEnumerable<Ticket> Items, int TotalCount)> GetAdminTicketsAsync(
        string? status,
        string? priority,
        int? userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// شمارش تیکت‌های باز کاربر
    /// </summary>
    Task<int> CountOpenByUserIdAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// شمارش تیکت‌های در انتظار پاسخ
    /// </summary>
    Task<int> CountAwaitingReplyAsync(CancellationToken ct = default);

    /// <summary>
    /// بررسی دسترسی کاربر به تیکت
    /// </summary>
    Task<bool> UserHasAccessAsync(int ticketId, int userId, CancellationToken ct = default);

    /// <summary>
    /// افزودن تیکت جدید
    /// </summary>
    Task AddAsync(Ticket ticket, CancellationToken ct = default);

    /// <summary>
    /// به‌روزرسانی تیکت
    /// </summary>
    void Update(Ticket ticket);
}