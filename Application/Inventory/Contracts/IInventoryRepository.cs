namespace Application.Inventory.Contracts;

/// <summary>
/// Repository Interface برای InventoryTransaction
/// فقط عملیات‌های ضروری برای Persistence - بدون Business Logic
/// </summary>
public interface IInventoryRepository
{
    /// <summary>
    /// افزودن تراکنش موجودی
    /// </summary>
    Task AddTransactionAsync(
        InventoryTransaction transaction,
        CancellationToken ct = default
        );

    /// <summary>
    /// افزودن چند تراکنش موجودی
    /// </summary>
    Task AddTransactionsAsync(
        IEnumerable<InventoryTransaction> transactions,
        CancellationToken ct = default
        );

    /// <summary>
    /// دریافت تراکنش با شناسه
    /// </summary>
    Task<InventoryTransaction?> GetByIdAsync(
        int id,
        CancellationToken ct = default
        );

    /// <summary>
    /// دریافت تراکنش‌ها با فیلتر (برای Query Service)
    /// </summary>
    Task<(IEnumerable<InventoryTransaction> Items, int TotalCount)> GetTransactionsAsync(
        int? variantId,
        string? transactionType,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken ct = default
        );

    /// <summary>
    /// دریافت تراکنش‌های یک واریانت
    /// </summary>
    Task<IEnumerable<InventoryTransaction>> GetByVariantIdAsync(
        int variantId,
        CancellationToken ct = default
        );

    /// <summary>
    /// محاسبه موجودی از روی تراکنش‌ها (برای انبارگردانی)
    /// </summary>
    Task<int> CalculateStockFromTransactionsAsync(
        int variantId,
        CancellationToken ct = default
        );

    /// <summary>
    /// دریافت آخرین تراکنش یک واریانت
    /// </summary>
    Task<InventoryTransaction?> GetLastTransactionAsync(
        int variantId,
        CancellationToken ct = default
        );

    /// <summary>
    /// دریافت آمار موجودی
    /// </summary>
    Task<InventoryStatistics> GetStatisticsAsync(
        CancellationToken ct = default
        );

    /// <summary>
    /// دریافت تراکنش‌های یک آیتم سفارش
    /// </summary>
    Task<IEnumerable<InventoryTransaction>> GetByOrderItemIdAsync(
        int orderItemId,
        CancellationToken ct = default
        );

    /// <summary>
    /// به‌روزرسانی تراکنش
    /// </summary>
    void Update(
        InventoryTransaction transaction
        );

    Task AddAsync(
    InventoryTransaction transaction,
    CancellationToken ct
        );

    /// <summary>
    /// دریافت واریانت با قفل سطری برای جلوگیری از Race Condition
    /// عملیات قفل‌گذاری باید در سطح Aggregate Root انجام شود
    /// </summary>
    Task<ProductVariant?> GetVariantWithLockAsync(int variantId, CancellationToken ct = default);
}