namespace Infrastructure.Inventory.Services;

public class StockLedgerQueryService : IStockLedgerQueryService
{
    private readonly Persistence.Context.DBContext _context;
    private readonly ILogger<StockLedgerQueryService> _logger;

    public StockLedgerQueryService(Persistence.Context.DBContext context, ILogger<StockLedgerQueryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> GetCurrentBalanceAsync(int variantId, int? warehouseId = null, CancellationToken ct = default)
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
        int variantId, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
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