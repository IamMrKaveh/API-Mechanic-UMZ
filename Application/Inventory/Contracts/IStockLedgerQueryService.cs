using Application.Inventory.Features.Shared;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Contracts;

public interface IStockLedgerQueryService
{
    Task<PaginatedResult<StockLedgerEntryDto>> GetByVariantIdAsync(
        VariantId variantId,
        int page,
        int pageSize,
        CancellationToken ct = default);
}