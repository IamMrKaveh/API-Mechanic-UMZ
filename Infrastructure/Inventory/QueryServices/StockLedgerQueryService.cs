using Domain.Inventory.Entities;

namespace Infrastructure.Inventory.QueryServices;

public class StockLedgerQueryService(DBContext context) : IStockLedgerQueryService
{
    private readonly DBContext _context = context;

    public async Task<int> GetCurrentBalanceAsync(
        int variantId,
        int? warehouseId = null,
        CancellationToken ct = default)
    {
        var query = _context.StockLedgerEntries.Where(e => e.VariantId == variantId);
        if (warehouseId.HasValue)
            query = query.Where(e => e.WarehouseId == warehouseId);

        var lastEntry = await query
            .OrderByDescending(e => e.Id)
            .FirstOrDefaultAsync(ct);

        return lastEntry?.BalanceAfter ?? 0;
    }

    public async Task<IEnumerable<StockLedgerEntry>> GetLedgerAsync(
        int variantId,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = _context.StockLedgerEntries
            .Where(e => e.VariantId == variantId);

        if (from.HasValue)
            query = query.Where(e => e.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.CreatedAt <= to.Value);

        return await query
            .OrderByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }
}