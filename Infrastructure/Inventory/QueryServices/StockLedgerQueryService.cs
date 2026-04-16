using Application.Inventory.Contracts;
using Application.Inventory.Features.Shared;
using Domain.Inventory.Entities;
using Domain.Variant.ValueObjects;
using Infrastructure.Persistence.Context;
using Mapster;

namespace Infrastructure.Inventory.QueryServices;

public sealed class StockLedgerQueryService(DBContext context) : IStockLedgerQueryService
{
    public async Task<PaginatedResult<StockLedgerEntryDto>> GetByVariantIdAsync(
        VariantId variantId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.StockLedgerEntries
            .AsNoTracking()
            .Where(e => e.VariantId == variantId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return PaginatedResult<StockLedgerEntryDto>.Create(
            items.Adapt<List<StockLedgerEntryDto>>(), total, page, pageSize);
    }
}