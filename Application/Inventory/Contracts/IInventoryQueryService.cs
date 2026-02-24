namespace Application.Inventory.Contracts;

public interface IInventoryQueryService
{
    Task<InventoryStatusDto?> GetInventoryStatusAsync(int variantId, CancellationToken ct = default);

    Task<VariantStockStatusDto?> GetVariantStatusAsync(int variantId, CancellationToken ct = default);

    Task<IEnumerable<LowStockItemDto>> GetLowStockProductsAsync(int threshold, CancellationToken ct = default);

    Task<IEnumerable<OutOfStockItemDto>> GetOutOfStockProductsAsync(CancellationToken ct = default);

    Task<PaginatedResult<InventoryTransactionDto>> GetTransactionsAsync(
        int? variantId, string? transactionType, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken ct = default);

    Task<InventoryStatisticsDto> GetStatisticsAsync(CancellationToken ct = default);
}