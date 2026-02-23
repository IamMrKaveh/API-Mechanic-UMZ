namespace Application.Discount.Contracts;

/// <summary>
/// Repository Interface برای DiscountCode Aggregate
/// فقط عملیات‌های ضروری برای Persistence
/// </summary>
public interface IDiscountRepository
{
    /// <summary>
    /// دریافت کد تخفیف با شناسه
    /// </summary>
    Task<DiscountCode?> GetByIdAsync(
        int id,
        CancellationToken ct = default
        );

    /// <summary>
    /// دریافت کد تخفیف با کد
    /// </summary>
    Task<DiscountCode?> GetByCodeAsync(
        string code,
        CancellationToken ct = default
        );

    /// <summary>
    /// دریافت کد تخفیف به همراه Restrictions و Usages
    /// </summary>
    Task<DiscountCode?> GetByIdWithDetailsAsync(
        int id,
        CancellationToken ct = default
        );

    /// <summary>
    /// دریافت کد تخفیف به همراه Usages بر اساس orderId
    /// </summary>
    Task<DiscountCode?> GetByIdWithUsagesAsync(
        int id,
        CancellationToken ct = default
        );

    /// <summary>
    /// بررسی وجود کد تکراری
    /// </summary>
    Task<bool> ExistsByCodeAsync(
        string code,
        int? excludeId = null,
        CancellationToken ct = default
        );

    /// <summary>
    /// دریافت لیست صفحه‌بندی شده
    /// </summary>
    Task<(IEnumerable<DiscountCode> Discounts, int TotalCount)> GetPagedAsync(
        bool includeExpired,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default
        );

    /// <summary>
    /// دریافت تعداد استفاده کاربر از یک کد تخفیف
    /// </summary>
    Task<int> CountUserUsageAsync(
        int discountId,
        int userId,
        CancellationToken ct = default
        );

    /// <summary>
    /// دریافت کدهای تخفیف فعال
    /// </summary>
    Task<IEnumerable<DiscountCode>> GetActiveDiscountsAsync(
        CancellationToken ct = default
        );

    /// <summary>
    /// دریافت کدهای تخفیف در حال انقضا
    /// </summary>
    Task<IEnumerable<DiscountCode>> GetExpiringDiscountsAsync(
        DateTime beforeDate,
        CancellationToken ct = default
        );

    /// <summary>
    /// دریافت استفاده بر اساس سفارش
    /// </summary>
    Task<DiscountUsage?> GetUsageByOrderIdAsync(
        int orderId,
        CancellationToken ct = default
        );

    /// <summary>
    /// افزودن کد تخفیف جدید
    /// </summary>
    Task AddAsync(
        DiscountCode discount,
        CancellationToken ct = default
        );

    /// <summary>
    /// به‌روزرسانی کد تخفیف
    /// </summary>
    void Update(
        DiscountCode discount
        );

    /// <summary>
    /// تنظیم RowVersion برای کنترل همزمانی
    /// </summary>
    void SetOriginalRowVersion(
        DiscountCode entity,
        byte[] rowVersion
        );
}