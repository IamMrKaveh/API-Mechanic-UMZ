using Application.Common.Interfaces.Cache;
using Application.Common.Interfaces.Inventory;

namespace Application.Services.Inventory;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICacheService _cacheService;
    private readonly LedkaContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InventoryService> _logger;

    private const string ProductCachePrefix = "product:";
    private const string ProductTagPrefix = "product_tag:";
    private const string ProductListCachePrefix = "products:list:";

    public InventoryService(
        IInventoryRepository inventoryRepository,
        IProductRepository productRepository,
        ICacheService cacheService,
        LedkaContext context,
        IUnitOfWork unitOfWork,
        ILogger<InventoryService> logger)
    {
        _inventoryRepository = inventoryRepository;
        _productRepository = productRepository;
        _cacheService = cacheService;
        _context = context;
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

        if (rowVersion != null)
        {
            _productRepository.SetVariantRowVersion(variant, rowVersion);
        }

        var stockBefore = variant.StockQuantity;

        variant.AdjustStock(quantityChange);

        var transaction = new InventoryTransaction
        {
            VariantId = variantId,
            TransactionType = transactionType,
            QuantityChange = quantityChange,
            StockBefore = stockBefore,
            OrderItemId = orderItemId,
            UserId = userId,
            Notes = notes,
            ReferenceNumber = referenceNumber,
            CreatedAt = DateTime.UtcNow
        };

        await _inventoryRepository.AddTransactionAsync(transaction);
        _productRepository.UpdateVariant(variant);

        if (saveChanges)
        {
            await _unitOfWork.SaveChangesAsync();

            await InvalidateProductCacheAsync(variant.ProductId);
        }

        _logger.LogInformation(
            "Inventory transaction logged: Variant {VariantId}, Type {Type}, Change {Change}, StockBefore {Before}, StockAfter {After}",
            variantId, transactionType, quantityChange, stockBefore, variant.StockQuantity);
    }

    public async Task RollbackReservationsAsync(string referenceNumber)
    {
        try
        {
            var reservations = await _context.InventoryTransactions
                .Where(t => t.ReferenceNumber == referenceNumber &&
                           (t.TransactionType == "Reservation" || t.TransactionType == "Sale"))
                .Include(t => t.Variant)
                .ToListAsync();

            var affectedProductIds = new HashSet<int>();

            foreach (var reservation in reservations)
            {
                if (reservation.Variant != null && !reservation.Variant.IsUnlimited)
                {
                    reservation.Variant.AdjustStock(-reservation.QuantityChange);

                    var rollbackTransaction = new InventoryTransaction
                    {
                        VariantId = reservation.VariantId,
                        TransactionType = "ReservationRollback",
                        QuantityChange = -reservation.QuantityChange,
                        StockBefore = reservation.Variant.StockQuantity + reservation.QuantityChange,
                        OrderItemId = reservation.OrderItemId,
                        UserId = reservation.UserId,
                        Notes = $"Rollback for {referenceNumber}",
                        ReferenceNumber = $"ROLLBACK-{referenceNumber}",
                        CreatedAt = DateTime.UtcNow
                    };

                    await _inventoryRepository.AddTransactionAsync(rollbackTransaction);
                    _productRepository.UpdateVariant(reservation.Variant);

                    affectedProductIds.Add(reservation.Variant.ProductId);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            foreach (var productId in affectedProductIds)
            {
                await InvalidateProductCacheAsync(productId);
            }

            _logger.LogInformation("Rolled back reservations for reference: {ReferenceNumber}", referenceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back reservations for reference: {ReferenceNumber}", referenceNumber);
            throw;
        }
    }

    public async Task<bool> ReconcileStockAsync(int variantId)
    {
        var variant = await _productRepository.GetVariantByIdForUpdateAsync(variantId);
        if (variant == null)
        {
            _logger.LogError("Variant {VariantId} not found for reconciliation", variantId);
            return false;
        }

        var calculatedStock = variant.CalculateStockFromTransactions();

        if (variant.StockQuantity != calculatedStock)
        {
            _logger.LogWarning(
                "Stock inconsistency detected for variant {VariantId}.   Stored: {Stored}, Calculated: {Calculated}",
                variantId, variant.StockQuantity, calculatedStock);

            var difference = calculatedStock - variant.StockQuantity;

            var adjustmentTransaction = new InventoryTransaction
            {
                VariantId = variantId,
                TransactionType = "Reconciliation",
                QuantityChange = difference,
                StockBefore = variant.StockQuantity,
                UserId = null,
                Notes = $"Automatic reconciliation.   Stored: {variant.StockQuantity}, Calculated: {calculatedStock}",
                ReferenceNumber = $"RECON-{DateTime.UtcNow:yyyyMMddHHmmss}",
                CreatedAt = DateTime.UtcNow
            };

            variant.StockQuantity = calculatedStock;

            await _inventoryRepository.AddTransactionAsync(adjustmentTransaction);
            _productRepository.UpdateVariant(variant);
            await _unitOfWork.SaveChangesAsync();

            await InvalidateProductCacheAsync(variant.ProductId);

            return true;
        }

        return true;
    }

    private async Task InvalidateProductCacheAsync(int productId)
    {
        await _cacheService.ClearAsync($"{ProductCachePrefix}{productId}");
        await _cacheService.ClearByTagAsync($"{ProductTagPrefix}{productId}");
        await _cacheService.ClearByPrefixAsync(ProductListCachePrefix);

        _logger.LogDebug("Product cache invalidated for product {ProductId} due to inventory change", productId);
    }
}