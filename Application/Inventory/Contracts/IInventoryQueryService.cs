using Application.Inventory.Features.Shared;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Contracts;

public interface IInventoryQueryService
{
    Task<InventoryDto?> GetByVariantIdAsync(
        VariantId variantId,
        CancellationToken ct = default);

    Task<IReadOnlyList<VariantAvailabilityDto>> GetBatchAvailabilityAsync(
        ICollection<VariantId> variantIds,
        CancellationToken ct = default);

    Task<PaginatedResult<InventoryTransactionDto>> GetTransactionsPagedAsync(
        VariantId? variantId,
        string? transactionType,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IEnumerable<LowStockItemDto>> GetLowStockProductsAsync(
        int threshold,
        CancellationToken ct = default);

    Task<IEnumerable<OutOfStockItemDto>> GetOutOfStockProductsAsync(
        CancellationToken ct = default);

    Task<InventoryStatisticsDto?> GetStatisticsAsync(
        CancellationToken ct = default);

    Task<InventoryStatusDto?> GetInventoryStatusAsync(
        VariantId variantId,
        CancellationToken ct = default);

    Task<IEnumerable<WarehouseStockDto>> GetWarehouseStockByVariantAsync(
        VariantId variantId,
        CancellationToken ct = default);
}