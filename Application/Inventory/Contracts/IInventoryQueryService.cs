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
}