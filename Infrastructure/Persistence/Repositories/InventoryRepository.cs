namespace Infrastructure.Persistence.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly LedkaContext _context;

    public InventoryRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<Domain.Product.ProductVariant?> GetVariantByIdAsync(int variantId)
    {
        return await _context.Set<Domain.Product.ProductVariant>().FindAsync(variantId);
    }

    public void SetVariantRowVersion(Domain.Product.ProductVariant variant, byte[] rowVersion)
    {
        _context.Entry(variant).Property("RowVersion").OriginalValue = rowVersion;
    }

    public async Task AddTransactionAsync(Domain.Inventory.InventoryTransaction transaction)
    {
        await _context.Set<Domain.Inventory.InventoryTransaction>().AddAsync(transaction);
    }

    public async Task<(IEnumerable<Domain.Inventory.InventoryTransaction> transactions, int total)> GetTransactionsAsync(int variantId, int page, int pageSize)
    {
        var query = _context.Set<Domain.Inventory.InventoryTransaction>()
            .Where(t => t.VariantId == variantId)
            .OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync();
        var transactions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (transactions, total);
    }
}