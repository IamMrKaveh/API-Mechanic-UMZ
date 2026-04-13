using Application.Inventory.Features.Shared;
using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Contracts;

public interface IInventoryQueryService
{
    Task<InventoryDto?> GetByVariantIdAsync(
        VariantId variantId,
        CancellationToken ct = default);

    Task<PaginatedResult<InventoryDto>> GetLowStockAsync(
        StockQuantity threshold,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<PaginatedResult<InventoryDto>> GetOutOfStockAsync(
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<InventoryDto>> GetByVariantIdsAsync(
        IEnumerable<VariantId> variantIds,
        CancellationToken ct = default);

    Task<InventoryDto?> GetVariantAvailabilityAsync(
        VariantId variantId,
        CancellationToken ct = default);

    Task<InventoryDto?> GetVariantStatusAsync(
        VariantId variantId,
        CancellationToken ct = default);

    Task<IReadOnlyList<VariantAvailabilityDto>> GetBatchAvailabilityAsync(
        IList<VariantId> variantIds,
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