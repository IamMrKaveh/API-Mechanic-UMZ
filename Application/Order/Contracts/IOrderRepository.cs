namespace Application.Order.Contracts;

public interface IOrderRepository
{
    /// <summary>
    /// دریافت سفارش بر اساس شناسه
    /// </summary>
    Task<Domain.Order.Order?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// دریافت سفارش به همراه آیتم‌ها
    /// </summary>
    Task<Domain.Order.Order?> GetByIdWithItemsAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// دریافت سفارش بر اساس کلید Idempotency
    /// </summary>
    Task<Domain.Order.Order?> GetByIdempotencyKeyAsync(string key, int userId, CancellationToken ct = default);

    /// <summary>
    /// بررسی وجود کلید Idempotency
    /// </summary>
    Task<bool> ExistsByIdempotencyKeyAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// دریافت سفارشات پرداخت نشده منقضی
    /// </summary>
    Task<IEnumerable<Domain.Order.Order>> GetExpiredUnpaidOrdersAsync(
        DateTime cutoffTime,
        int maxCount,
        CancellationToken ct = default);

    /// <summary>
    /// افزودن سفارش جدید
    /// </summary>
    Task AddAsync(Domain.Order.Order order, CancellationToken ct = default);

    /// <summary>
    /// به‌روزرسانی سفارش
    /// </summary>
    void Update(Domain.Order.Order order);

    /// <summary>
    /// تنظیم RowVersion اصلی برای Concurrency Control
    /// </summary>
    void SetOriginalRowVersion(Domain.Order.Order entity, byte[] rowVersion);

    Task<Domain.Order.Order?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default);

    Task<Domain.Order.Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default);

    Task<bool> HasActiveOrdersAsync(int userId, CancellationToken ct = default);
}