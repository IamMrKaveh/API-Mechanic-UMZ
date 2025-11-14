namespace Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<InventoryService> logger)
    {
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task LogTransactionAsync(int variantId, string transactionType, int quantityChange, int? orderItemId, int? userId, string? notes, string? referenceNumber = null, byte[]? rowVersion = null)
    {
        var variant = await _inventoryRepository.GetVariantByIdAsync(variantId);
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
            _inventoryRepository.SetVariantRowVersion(variant, rowVersion);
        }

        var stockBefore = variant.Stock;
        var stockAfter = stockBefore + quantityChange;

        if (stockAfter < 0)
        {
            throw new InvalidOperationException($"Insufficient stock for variant {variantId}. Current stock: {stockBefore}, Requested change: {quantityChange}");
        }

        variant.Stock = stockAfter;

        var transaction = new Domain.Inventory.InventoryTransaction
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

        await _inventoryRepository.AddTransactionAsync(transaction);

        _logger.LogInformation("Inventory transaction prepared for Variant {VariantId}. Type: {Type}, Change: {Change}, New Stock: {NewStock}",
            variantId, transactionType, quantityChange, stockAfter);
    }

    public async Task<(IEnumerable<Domain.Inventory.InventoryTransaction> transactions, int total)> GetTransactionsAsync(int variantId, int page, int pageSize)
    {
        return await _inventoryRepository.GetTransactionsAsync(variantId, page, pageSize);
    }

    public async Task<int> GetCurrentStockAsync(int variantId)
    {
        var variant = await _inventoryRepository.GetVariantByIdAsync(variantId);
        return variant?.Stock ?? 0;
    }

    public async Task<(bool success, string message)> AdjustStockAsync(int variantId, int newStock, int userId, string notes)
    {
        var maxRetries = 3;
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                var variant = await _inventoryRepository.GetVariantByIdAsync(variantId);
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
                await _unitOfWork.SaveChangesAsync();

                return (true, "Stock adjusted successfully.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict on attempt {Attempt} for variant {VariantId}. Retrying...", i + 1, variantId);
                if (i == maxRetries - 1)
                {
                    return (false, "Could not adjust stock due to repeated concurrency conflicts.");
                }
            }
        }
        return (false, "Failed to adjust stock after multiple retries.");
    }
}