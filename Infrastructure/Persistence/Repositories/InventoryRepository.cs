namespace Infrastructure.Persistence.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly LedkaContext _context;

    public InventoryRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<ProductVariant?> GetVariantByIdAsync(int variantId)
    {
        return await _context.ProductVariants
            .Include(v => v.InventoryTransactions)
            .FirstOrDefaultAsync(v => v.Id == variantId);
    }

    public void SetVariantRowVersion(ProductVariant variant, byte[] rowVersion)
    {
        _context.Entry(variant).Property(v => v.RowVersion).OriginalValue = rowVersion;
    }

    public async Task AddTransactionAsync(InventoryTransaction transaction)
    {
        await _context.InventoryTransactions.AddAsync(transaction);
    }

    public async Task<(IEnumerable<InventoryTransaction> transactions, int total)> GetTransactionsAsync(int variantId, int page, int pageSize)
    {
        var query = _context.InventoryTransactions
            .Where(t => t.VariantId == variantId)
            .OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync();
        var transactions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(t => t.User)
            .Include(t => t.OrderItem)
            .ToListAsync();

        return (transactions, total);
    }
}