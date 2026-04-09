using Application.Inventory.Features.Shared;

namespace Application.Inventory.Contracts;

public interface IStockLedgerQueryService
{
    Task<PaginatedResult<StockLedgerEntryDto>> GetByVariantIdAsync(
        Guid variantId,
        int page,
        int pageSize,
        CancellationToken ct = default);
}