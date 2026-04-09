using Application.Inventory.Features.Shared;

namespace Application.Inventory.Contracts;

public interface IInventoryQueryService
{
    Task<InventoryDto?> GetByVariantIdAsync(Guid variantId, CancellationToken ct = default);

    Task<PaginatedResult<InventoryDto>> GetLowStockAsync(
        int threshold,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<PaginatedResult<InventoryDto>> GetOutOfStockAsync(
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<InventoryDto>> GetByVariantIdsAsync(
        IEnumerable<Guid> variantIds,
        CancellationToken ct = default);
}