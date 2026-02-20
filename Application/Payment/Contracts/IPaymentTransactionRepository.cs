namespace Application.Payment.Contracts;

/// <summary>
/// Repository Interface برای PaymentTransaction Aggregate
/// فقط عملیات‌های ضروری برای Persistence - بدون Business Logic
/// </summary>
public interface IPaymentTransactionRepository
{
    /// <summary>
    /// افزودن تراکنش جدید
    /// </summary>
    Task AddAsync(PaymentTransaction transaction, CancellationToken ct = default);

    /// <summary>
    /// دریافت تراکنش با شناسه
    /// </summary>
    Task<PaymentTransaction?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// دریافت تراکنش با Authority
    /// </summary>
    Task<PaymentTransaction?> GetByAuthorityAsync(string authority, CancellationToken ct = default);

    /// <summary>
    /// دریافت تراکنش با Authority به همراه سفارش
    /// </summary>
    Task<PaymentTransaction?> GetByAuthorityWithOrderAsync(string authority, CancellationToken ct = default);

    /// <summary>
    /// دریافت تراکنش‌های در انتظار که منقضی شده‌اند
    /// </summary>
    Task<IEnumerable<PaymentTransaction>> GetPendingExpiredTransactionsAsync(
        DateTime cutoffTime,
        CancellationToken ct = default);

    /// <summary>
    /// دریافت لیست صفحه‌بندی شده
    /// </summary>
    Task<(IEnumerable<PaymentTransaction> Transactions, int TotalCount)> GetPagedAsync(
        int? orderId = null,
        int? userId = null,
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    /// <summary>
    /// دریافت تراکنش‌های یک سفارش
    /// </summary>
    Task<IEnumerable<PaymentTransaction>> GetByOrderIdAsync(
        int orderId,
        CancellationToken ct = default);

    /// <summary>
    /// دریافت تراکنش‌های موفق یک سفارش
    /// </summary>
    Task<IEnumerable<PaymentTransaction>> GetSuccessfulByOrderIdAsync(
        int orderId,
        CancellationToken ct = default);

    /// <summary>
    /// بررسی وجود تراکنش موفق برای سفارش
    /// </summary>
    Task<bool> HasSuccessfulPaymentAsync(int orderId, CancellationToken ct = default);

    /// <summary>
    /// دریافت آخرین تراکنش سفارش
    /// </summary>
    Task<PaymentTransaction?> GetLatestByOrderIdAsync(int orderId, CancellationToken ct = default);

    /// <summary>
    /// بررسی وجود تراکنش در انتظار برای سفارش
    /// </summary>
    Task<bool> HasPendingPaymentAsync(int orderId, CancellationToken ct = default);

    /// <summary>
    /// دریافت آمار پرداخت‌ها
    /// </summary>
    Task<PaymentStatistics> GetStatisticsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default);

    /// <summary>
    /// به‌روزرسانی تراکنش
    /// </summary>
    void Update(PaymentTransaction transaction);

    /// <summary>
    /// تنظیم RowVersion برای کنترل همزمانی
    /// </summary>
    void SetOriginalRowVersion(PaymentTransaction entity, byte[] rowVersion);

    Task<PaymentTransaction?> GetVerifiedByOrderIdAsync(int orderId, CancellationToken ct = default);

    Task<PaymentTransaction?> GetActiveByOrderIdAsync(int orderId, CancellationToken ct = default);
}