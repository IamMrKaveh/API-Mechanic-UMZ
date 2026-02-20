namespace Application.Inventory.Contracts;

/// <summary>
/// Read-side Query Service برای Inventory
/// مستقیماً DTO برمی‌گرداند - بدون عبور از Domain Model
/// </summary>
public interface IInventoryQueryService
{
    /// <summary>
    /// وضعیت موجودی یک واریانت
    /// </summary>
    Task<InventoryStatusDto?> GetInventoryStatusAsync(int variantId, CancellationToken ct = default);

    /// <summary>
    /// وضعیت real-time واریانت برای Availability endpoint
    /// </summary>
    Task<VariantStockStatusDto?> GetVariantStatusAsync(int variantId, CancellationToken ct = default);

    /// <summary>
    /// محصولات با موجودی کم
    /// </summary>
    Task<IEnumerable<LowStockItemDto>> GetLowStockProductsAsync(int threshold, CancellationToken ct = default);

    /// <summary>
    /// محصولات ناموجود
    /// </summary>
    Task<IEnumerable<OutOfStockItemDto>> GetOutOfStockProductsAsync(CancellationToken ct = default);

    /// <summary>
    /// تاریخچه تراکنش‌ها با فیلتر
    /// </summary>
    Task<PaginatedResult<InventoryTransactionDto>> GetTransactionsAsync(
        int? variantId,
        string? transactionType,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// آمار انبار
    /// </summary>
    Task<InventoryStatisticsDto> GetStatisticsAsync(CancellationToken ct = default);
}

/// <summary>
/// DTO برای وضعیت موجودی real-time
/// </summary>
public class VariantStockStatusDto
{
    public int VariantId { get; set; }
    public int StockQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableStock { get; set; }
    public bool IsInStock { get; set; }
    public bool IsUnlimited { get; set; }
}