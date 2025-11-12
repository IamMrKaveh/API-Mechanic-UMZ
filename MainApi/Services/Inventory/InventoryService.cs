namespace MainApi.Services.Inventory;

public class InventoryService : IInventoryService
{
    private readonly MechanicContext _context;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(MechanicContext context, ILogger<InventoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogTransactionAsync(int variantId, string transactionType, int quantityChange, int? orderItemId, int? userId, string? notes, string? referenceNumber, byte[]? rowVersion = null)
    {
        var variant = await _context.TProductVariant.FindAsync(variantId);
        if (variant == null)
        {
            _logger.LogError("Cannot log inventory transaction: Variant with ID {VariantId} not found.", variantId);
            throw new KeyNotFoundException($"Variant with ID {variantId} not found.");
        }

        if (variant.IsUnlimited)
        {
            _logger.LogInformation("Skipping inventory transaction log for unlimited stock variant {VariantId}.", variantId);
            return;
        }

        if (rowVersion != null)
        {
            _context.Entry(variant).Property("RowVersion").OriginalValue = rowVersion;
        }

        var stockBefore = variant.Stock;
        var stockAfter = stockBefore + quantityChange;

        if (stockAfter < 0)
        {
            throw new InvalidOperationException($"Insufficient stock for variant {variantId}. Current stock: {stockBefore}, Requested change: {quantityChange}");
        }

        variant.Stock = stockAfter;

        var transaction = new TInventoryTransaction
        {
            VariantId = variantId,
            TransactionType = transactionType,
            Quantity = quantityChange,
            StockBefore = stockBefore,
            StockAfter = stockAfter,
            OrderItemId = orderItemId,
            UserId = userId,
            Notes = notes,
            ReferenceNumber = referenceNumber,
            CreatedAt = DateTime.UtcNow
        };

        _context.TInventoryTransaction.Add(transaction);

        _logger.LogInformation("Inventory transaction prepared for Variant {VariantId}. Type: {Type}, Change: {Change}, New Stock: {NewStock}",
            variantId, transactionType, quantityChange, stockAfter);
    }

    public async Task<(IEnumerable<TInventoryTransaction> transactions, int total)> GetTransactionsAsync(int variantId, int page, int pageSize)
    {
        var query = _context.TInventoryTransaction
            .Where(t => t.VariantId == variantId)
            .OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync();
        var transactions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (transactions, total);
    }

    public async Task<int> GetCurrentStockAsync(int variantId)
    {
        var variant = await _context.TProductVariant
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == variantId);

        return variant?.Stock ?? 0;
    }

    public async Task<(bool success, string message)> AdjustStockAsync(int variantId, int newStock, int userId, string notes)
    {
        var maxRetries = 3;
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                var variant = await _context.TProductVariant.FindAsync(variantId);
                if (variant == null)
                {
                    return (false, $"Variant with ID {variantId} not found.");
                }
                if (variant.IsUnlimited)
                {
                    return (false, "Cannot adjust stock for an unlimited product.");
                }

                var quantityChange = newStock - variant.Stock;
                if (quantityChange == 0)
                {
                    return (true, "Stock is already at the desired level. No adjustment made.");
                }

                await LogTransactionAsync(variantId, "Adjustment", quantityChange, null, userId, notes, null, variant.RowVersion);
                await _context.SaveChangesAsync();

                return (true, "Stock adjusted successfully.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict on attempt {Attempt} for variant {VariantId}. Retrying...", i + 1, variantId);
                foreach (var entry in ex.Entries)
                {
                    await entry.ReloadAsync();
                }

                if (i == maxRetries - 1)
                {
                    return (false, "Could not adjust stock due to repeated concurrency conflicts.");
                }
            }
        }
        return (false, "Failed to adjust stock after multiple retries.");
    }
}