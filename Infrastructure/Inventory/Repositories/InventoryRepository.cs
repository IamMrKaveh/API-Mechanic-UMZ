using Domain.Inventory.Entities;
using Domain.Inventory.Interfaces;
using Domain.Variant.Aggregates;

namespace Infrastructure.Inventory.Repositories;

public class InventoryRepository(DBContext context) : IInventoryRepository
{
    private readonly DBContext _context = context;

    public async Task AddTransactionAsync(InventoryTransaction transaction, CancellationToken ct = default)
    {
        await _context.InventoryTransactions.AddAsync(transaction, ct);
    }

    public async Task AddTransactionsAsync(IEnumerable<InventoryTransaction> transactions, CancellationToken ct = default)
    {
        await _context.InventoryTransactions.AddRangeAsync(transactions, ct);
    }

    public void Update(InventoryTransaction transaction)
    {
        _context.InventoryTransactions.Update(transaction);
    }

    public async Task<ProductVariant?> ReverseTransactionAsync(int transactionId, int adminUserId, string reason, CancellationToken ct = default)
    {
        var transaction = await _context.InventoryTransactions
            .Include(t => t.Variant)
            .FirstOrDefaultAsync(t => t.Id == transactionId, ct);

        if (transaction == null) return null;

        transaction.MarkAsReversed();

        var variant = transaction.Variant;
        if (variant != null && !variant.IsUnlimited)
        {
            var reversalQty = -transaction.QuantityChange;
            variant.SetStock(variant.StockQuantity + reversalQty);
        }

        return variant;
    }

    public async Task<InventoryTransaction?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.InventoryTransactions
            .Include(t => t.Variant)
                .ThenInclude(v => v!.Product)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<IEnumerable<InventoryTransaction>> GetByVariantIdAsync(int variantId, CancellationToken ct = default)
    {
        return await _context.InventoryTransactions
            .Where(t => t.VariantId == variantId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<int> CalculateStockFromTransactionsAsync(int variantId, CancellationToken ct = default)
    {
        return await _context.InventoryTransactions
            .Where(t => t.VariantId == variantId && !t.IsReversed)
            .SumAsync(t => t.QuantityChange, ct);
    }

    public async Task<InventoryTransaction?> GetLastTransactionAsync(int variantId, CancellationToken ct = default)
    {
        return await _context.InventoryTransactions
            .Where(t => t.VariantId == variantId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IEnumerable<InventoryTransaction>> GetByOrderItemIdAsync(int orderItemId, CancellationToken ct = default)
    {
        return await _context.InventoryTransactions
            .Where(t => t.OrderItemId == orderItemId)
            .ToListAsync(ct);
    }

    public async Task<ProductVariant?> GetVariantWithLockAsync(int variantId, CancellationToken ct = default)
    {
        return await _context.ProductVariants
            .FromSqlRaw(
                "SELECT * FROM \"ProductVariants\" WHERE \"Id\" = {0} FOR UPDATE",
                variantId)
            .FirstOrDefaultAsync(ct);
    }
}