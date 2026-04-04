using Application.Inventory.Features.Shared;
using SharedKernel.Models;

namespace Application.Inventory.Contracts;

public interface IInventoryQueryService
{
    Task<InventoryStatusDto?> GetInventoryStatusAsync(int variantId, CancellationToken ct = default);

    Task<VariantStockStatusDto?> GetVariantStatusAsync(int variantId, CancellationToken ct = default);

    Task<IEnumerable<LowStockItemDto>> GetLowStockProductsAsync(int threshold, CancellationToken ct = default);

    Task<IEnumerable<OutOfStockItemDto>> GetOutOfStockProductsAsync(CancellationToken ct = default);

    Task<PaginatedResult<InventoryTransactionDto>> GetTransactionsPagedAsync(
        int? variantId,
        string? transactionType,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<InventoryStatisticsDto> GetStatisticsAsync(CancellationToken ct = default);

    Task<IEnumerable<WarehouseStockDto>> GetWarehouseStockByVariantAsync(int variantId, CancellationToken ct = default);
}