namespace Application.Inventory.Contracts;

/// <summary>
/// Application-level Inventory Service Interface
/// هماهنگی بین Domain Service، Repository و UnitOfWork
/// پیاده‌سازی در Infrastructure
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// رزرو موجودی برای سفارش (با Pessimistic Locking)
    /// </summary>
    Task<ServiceResult> ReserveStockAsync(
        int variantId,
        int quantity,
        int orderItemId,
        int? userId = null,
        string? referenceNumber = null,
        CancellationToken ct = default);

    /// <summary>
    /// تأیید رزرو پس از پرداخت موفق
    /// </summary>
    Task<ServiceResult> ConfirmReservationAsync(
        int variantId,
        int quantity,
        int orderItemId,
        int? userId = null,
        string? referenceNumber = null,
        CancellationToken ct = default);

    /// <summary>
    /// برگشت رزرو در صورت عدم پرداخت یا لغو
    /// </summary>
    Task<ServiceResult> RollbackReservationAsync(
        int variantId,
        int quantity,
        int? userId = null,
        string? reason = null,
        CancellationToken ct = default);

    /// <summary>
    /// تنظیم دستی موجودی (با Optimistic Concurrency)
    /// </summary>
    Task<ServiceResult> AdjustStockAsync(
        int variantId,
        int quantityChange,
        int userId,
        string notes,
        CancellationToken ct = default);

    /// <summary>
    /// ثبت خسارت
    /// </summary>
    Task<ServiceResult> RecordDamageAsync(
        int variantId,
        int quantity,
        int userId,
        string notes,
        CancellationToken ct = default);

    /// <summary>
    /// انبارگردانی - مقایسه و اصلاح اختلاف
    /// </summary>
    Task<ServiceResult<ReconcileResultDto>> ReconcileStockAsync(
        int variantId,
        int userId,
        CancellationToken ct = default);

    /// <summary>
    /// تنظیم دسته‌ای موجودی
    /// </summary>
    Task<ServiceResult<BulkAdjustResultDto>> BulkAdjustStockAsync(
        IEnumerable<BulkAdjustItemDto> items,
        int userId,
        CancellationToken ct = default);

    /// <summary>
    /// ثبت تراکنش موجودی (برای استفاده از سرویس‌های دیگر مثل PaymentCleanup)
    /// </summary>
    Task LogTransactionAsync(
        int variantId,
        string transactionType,
        int quantityChange,
        int? orderItemId,
        int? userId,
        string? notes = null,
        string? referenceNumber = null,
        int? stockBefore = null,
        bool saveChanges = true,
        CancellationToken ct = default);

    /// <summary>
    /// برگشت تمام رزروهای مرتبط با یک شماره مرجع (مثلاً ORDER-123)
    /// </summary>
    Task<ServiceResult> RollbackReservationsAsync(
        string referenceNumber,
        CancellationToken ct = default);
}

// DTOs مورد استفاده در Interface
public class ReconcileResultDto
{
    public int VariantId { get; set; }
    public int FinalStock { get; set; }
    public int Difference { get; set; }
    public bool HasDiscrepancy { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class BulkAdjustItemDto
{
    public int VariantId { get; set; }
    public int QuantityChange { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class BulkAdjustResultDto
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<BulkAdjustItemResultDto> Results { get; set; } = [];
}

public class BulkAdjustItemResultDto
{
    public int VariantId { get; set; }
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
    public int? NewStock { get; set; }
}