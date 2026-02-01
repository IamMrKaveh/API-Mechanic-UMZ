using Infrastructure.Persistence.Interface.Inventory;

namespace Infrastructure.Persistence.Repositories.Inventory;

public class InventoryTransactionRepository : IInventoryTransactionRepository
{
    private readonly LedkaContext _context;

    public InventoryTransactionRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task AddAsync(InventoryTransaction transaction)
    {
        await _context.InventoryTransactions.AddAsync(transaction);
    }

    public async Task<(IEnumerable<InventoryTransaction> transactions, int totalCount)> GetByVariantIdAsync(int variantId, int page, int pageSize)
    {
        var query = _context.InventoryTransactions
            .Where(t => t.VariantId == variantId)
            .Include(t => t.Variant)
                .ThenInclude(v => v.Product)
            .Include(t => t.User)
            .Include(t => t.OrderItem)
            .OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync();

        var transactions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (transactions, totalCount);
    }

    public async Task<IEnumerable<InventoryTransaction>> GetByReferenceNumberAsync(string referenceNumber)
    {
        return await _context.InventoryTransactions
            .Where(t => t.ReferenceNumber == referenceNumber)
            .Include(t => t.Variant)
            .Include(t => t.User)
            .Include(t => t.OrderItem)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetCurrentStockAsync(int variantId)
    {
        return await _context.InventoryTransactions
            .Where(t => t.VariantId == variantId)
            .SumAsync(t => t.QuantityChange);
    }
}