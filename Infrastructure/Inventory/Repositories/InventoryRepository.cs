namespace Infrastructure.Inventory.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly LedkaContext _context;

    public InventoryRepository(
        LedkaContext context
        )
    {
        _context = context;
    }

    public async Task AddTransactionAsync(
        InventoryTransaction transaction,
        CancellationToken ct = default
        )
    {
        await _context.InventoryTransactions.AddAsync(transaction, ct);
    }

    public async Task AddTransactionsAsync(
        IEnumerable<InventoryTransaction> transactions,
        CancellationToken ct = default
        )
    {
        await _context.InventoryTransactions.AddRangeAsync(transactions, ct);
    }

    public async Task<InventoryTransaction?> GetByIdAsync(
        int id,
        CancellationToken ct = default
        )
    {
        return await _context.InventoryTransactions
            .Include(t => t.Variant)
                .ThenInclude(v => v!.Product)
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<(IEnumerable<InventoryTransaction> Items, int TotalCount)> GetTransactionsAsync(
        int? variantId,
        string? transactionType,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken ct = default
        )
    {
        var query = _context.InventoryTransactions
            .Include(t => t.Variant)
                .ThenInclude(v => v!.Product)
            .Include(t => t.User)
            .AsQueryable();

        if (variantId.HasValue)
            query = query.Where(t => t.VariantId == variantId.Value);

        if (!string.IsNullOrWhiteSpace(transactionType))
            query = query.Where(t => t.TransactionType == transactionType);

        if (fromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(t => t.CreatedAt <= toDate.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IEnumerable<InventoryTransaction>> GetByVariantIdAsync(
        int variantId,
        CancellationToken ct = default
        )
    {
        return await _context.InventoryTransactions
            .Where(t => t.VariantId == variantId)
            .OrderByDescending(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<int> CalculateStockFromTransactionsAsync(
        int variantId,
        CancellationToken ct = default
        )
    {
        return await _context.InventoryTransactions
            .Where(t => t.VariantId == variantId && !t.IsReversed)
            .SumAsync(t => t.QuantityChange, ct);
    }

    public async Task<InventoryTransaction?> GetLastTransactionAsync(
        int variantId,
        CancellationToken ct = default
        )
    {
        return await _context.InventoryTransactions
            .Where(t => t.VariantId == variantId)
            .OrderByDescending(t => t.CreatedAt)
            .Include(t => t.User)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<InventoryStatistics> GetStatisticsAsync(
        CancellationToken ct = default
        )
    {
        var variants = await _context.Set<ProductVariant>()
            .Where(v => v.IsActive && !v.IsDeleted)
            .Select(v => new
            {
                v.Id,
                v.StockQuantity,
                v.ReservedQuantity,
                v.IsUnlimited,
                v.LowStockThreshold,
                v.PurchasePrice,
                v.SellingPrice
            })
            .AsNoTracking()
            .ToListAsync(ct);

        var totalVariants = variants.Count;
        var unlimitedVariants = variants.Count(v => v.IsUnlimited);

        var finiteVariants = variants.Where(v => !v.IsUnlimited).ToList();
        var outOfStockVariants = finiteVariants.Count(v => v.StockQuantity - v.ReservedQuantity <= 0);
        var lowStockVariants = finiteVariants.Count(v =>
        {
            var available = v.StockQuantity - v.ReservedQuantity;
            return available > 0 && available <= v.LowStockThreshold;
        });
        var inStockVariants = finiteVariants.Count(v => v.StockQuantity - v.ReservedQuantity > 0);

        var totalInventoryValue = finiteVariants.Sum(v => v.PurchasePrice * v.StockQuantity);
        var totalSellingValue = finiteVariants.Sum(v => v.SellingPrice * v.StockQuantity);

        return InventoryStatistics.Create(
            totalVariants,
            inStockVariants,
            lowStockVariants,
            outOfStockVariants,
            unlimitedVariants,
            totalInventoryValue,
            totalSellingValue);
    }

    public async Task<IEnumerable<InventoryTransaction>> GetByOrderItemIdAsync(
        int orderItemId,
        CancellationToken ct = default
        )
    {
        return await _context.InventoryTransactions
            .Where(t => t.OrderItemId == orderItemId)
            .OrderByDescending(t => t.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public void Update(
        InventoryTransaction transaction
        )
    {
        _context.InventoryTransactions.Update(transaction);
    }

    public async Task AddAsync(
        InventoryTransaction transaction,
        CancellationToken ct
        )
    {
        await _context.InventoryTransactions.AddAsync(
            transaction,
            ct);
    }

    public async Task<ProductVariant?> GetVariantWithLockAsync(
        int variantId,
        CancellationToken ct = default
        )
    {
        return await _context.Set<ProductVariant>()
            .FromSqlRaw("SELECT * FROM \"ProductVariants\" WHERE \"Id\" = {0} FOR UPDATE", variantId)
            .Include(v => v.Product)
            .FirstOrDefaultAsync(ct);
    }
}