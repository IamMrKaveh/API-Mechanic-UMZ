namespace Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        IInventoryRepository inventoryRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ILogger<InventoryService> logger)
    {
        _inventoryRepository = inventoryRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task LogTransactionAsync(
        int variantId,
        string transactionType,
        int quantityChange,
        int? orderItemId,
        int? userId,
        string notes,
        string? referenceNumber,
        byte[]? rowVersion = null,
        bool saveChanges = true)
    {
        var variant = await _productRepository.GetVariantByIdForUpdateAsync(variantId);
        if (variant == null)
        {
            _logger.LogError("Attempted to log inventory for non-existent variant {VariantId}", variantId);
            throw new ArgumentException("Variant not found.", nameof(variantId));
        }

        if (rowVersion != null) _productRepository.SetVariantRowVersion(variant, rowVersion);

        var stockBefore = variant.Stock;

        if (!variant.IsUnlimited && (stockBefore + quantityChange < 0))
        {
            _logger.LogWarning("Insufficient stock for variant {VariantId}. Requested: {QuantityChange}, Current: {StockBefore}", variantId, quantityChange, stockBefore);
            throw new InvalidOperationException("Insufficient stock.");
        }

        var transaction = new InventoryTransaction
        {
            VariantId = variantId,
            TransactionType = transactionType,
            QuantityChange = quantityChange,
            StockBefore = stockBefore,
            OrderItemId = orderItemId,
            UserId = userId,
            Notes = notes,
            ReferenceNumber = referenceNumber
        };

        await _inventoryRepository.AddTransactionAsync(transaction);

        if (saveChanges) await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Inventory tx prepared: Variant {VariantId}, Type {Type}, Change {Change}", variantId, transactionType, quantityChange);
    }

    public async Task RollbackReservationsAsync(string referenceNumber)
    {
        // Implementation would require querying transactions by ReferenceNumber and reversing them
        // For this prompt, placeholder ensuring the interface exists.
    }
}