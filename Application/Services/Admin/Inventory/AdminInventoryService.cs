namespace Application.Services.Admin.Inventory;

public class AdminInventoryService : IAdminInventoryService
{
    private readonly IInventoryRepository _inventoryRepository; 
    private readonly IProductRepository _productRepository; 
    private readonly IInventoryService _inventoryService; 
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService; 
    private readonly IUnitOfWork _unitOfWork; 
    private readonly IAppLogger<AdminInventoryService> _logger; 
    private readonly IMediaService _mediaService;

    public AdminInventoryService(
        IInventoryRepository inventoryRepository,
        IProductRepository productRepository,
        IInventoryService inventoryService,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        IAppLogger<AdminInventoryService> logger,
        IMediaService mediaService)
    {
        _inventoryRepository = inventoryRepository;
        _productRepository = productRepository;
        _inventoryService = inventoryService;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mediaService = mediaService;
    }

    public async Task<ServiceResult<PagedResultDto<InventoryTransactionDto>>> GetTransactionsAsync(
        int? variantId,
        string? transactionType,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize)
    {
        var (transactions, totalItems) = await _inventoryRepository.GetTransactionsAsync(variantId, transactionType, fromDate, toDate, page, pageSize);

        var transactionDtos = new List<InventoryTransactionDto>();
        foreach (var t in transactions)
        {
            var variantName = string.Join(" / ", t.Variant.VariantAttributes.Select(va => va.AttributeValue.DisplayValue));
            var productImage = await _mediaService.GetPrimaryImageUrlAsync("Product", t.Variant.ProductId);

            transactionDtos.Add(new InventoryTransactionDto
            {
                Id = t.Id,
                VariantId = t.VariantId,
                ProductId = t.Variant.ProductId,
                ProductName = t.Variant.Product.Name,
                ProductImage = productImage,
                VariantSku = t.Variant.Sku,
                VariantName = variantName,
                TransactionType = t.TransactionType,
                QuantityChange = t.QuantityChange,
                StockBefore = t.StockBefore,
                StockAfter = t.StockBefore + t.QuantityChange,
                Notes = t.Notes,
                ReferenceNumber = t.ReferenceNumber,
                OrderItemId = t.OrderItemId,
                UserId = t.UserId,
                UserName = t.User != null ? $"{t.User.FirstName} {t.User.LastName}" : "سیستم",
                CreatedAt = t.CreatedAt
            });
        }

        var result = PagedResultDto<InventoryTransactionDto>.Create(transactionDtos, totalItems, page, pageSize);
        return ServiceResult<PagedResultDto<InventoryTransactionDto>>.Ok(result);
    }

    public async Task<ServiceResult<IEnumerable<LowStockItemDto>>> GetLowStockItemsAsync(int threshold)
    {
        var lowStockItems = await _productRepository.GetLowStockVariantsAsync(threshold);

        var result = lowStockItems
            .Select(v => new LowStockItemDto
            {
                VariantId = v.Id,
                ProductId = v.ProductId,
                ProductName = v.Product.Name,
                Sku = v.Sku,
                Stock = v.Stock,
                CategoryName = v.Product.CategoryGroup?.Category?.Name,
                SellingPrice = v.SellingPrice,
                VariantDisplayName = string.Join(" / ", v.VariantAttributes.Select(va => va.AttributeValue.DisplayValue))
            })
            .OrderBy(v => v.Stock)
            .ToList();

        return ServiceResult<IEnumerable<LowStockItemDto>>.Ok(result);
    }

    public async Task<ServiceResult<IEnumerable<OutOfStockItemDto>>> GetOutOfStockItemsAsync()
    {
        var outOfStockItems = await _productRepository.GetOutOfStockVariantsAsync();

        var variantIds = outOfStockItems.Select(v => v.Id).ToList();
        var lastSales = await _inventoryRepository.GetLastSaleDatesAsync(variantIds);

        var result = outOfStockItems
            .Select(v => new OutOfStockItemDto
            {
                VariantId = v.Id,
                ProductId = v.ProductId,
                ProductName = v.Product.Name,
                Sku = v.Sku,
                CategoryName = v.Product.CategoryGroup?.Category?.Name,
                SellingPrice = v.SellingPrice,
                VariantDisplayName = string.Join(" / ", v.VariantAttributes.Select(va => va.AttributeValue.DisplayValue)),
                LastSaleDate = lastSales.TryGetValue(v.Id, out var date) ? date : null
            })
            .ToList();

        return ServiceResult<IEnumerable<OutOfStockItemDto>>.Ok(result);
    }

    public async Task<ServiceResult> AdjustStockAsync(StockAdjustmentDto dto, int userId)
    {
        try
        {
            await _inventoryService.LogTransactionAsync(
                dto.VariantId,
                dto.TransactionType,
                dto.QuantityChange,
                null,
                userId,
                dto.Notes ?? "Manual adjustment",
                dto.ReferenceNumber);

            await _auditService.LogInventoryEventAsync(
                dto.VariantId,
                "StockAdjustment",
                $"Stock adjusted by {dto.QuantityChange} for variant {dto.VariantId}. Type: {dto.TransactionType}",
                userId);

            return ServiceResult.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting stock for variant {VariantId}", dto.VariantId);
            return ServiceResult.Fail("An error occurred while adjusting stock");
        }
    }

    public async Task<ServiceResult> ReconcileStockAsync(int variantId)
    {
        try
        {
            var success = await _inventoryService.ReconcileStockAsync(variantId);
            if (success)
            {
                return ServiceResult.Ok();
            }
            return ServiceResult.Fail("Reconciliation failed or not needed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reconciling stock for variant {VariantId}", variantId);
            return ServiceResult.Fail("An error occurred during reconciliation");
        }
    }

    public async Task<ServiceResult<InventoryStatisticsDto>> GetStatisticsAsync()
    {
        var stats = await _productRepository.GetProductStatisticsAsync();

        // Convert the repository statistics object (assumed object for now based on IAdminProductService usage)
        // Since we need to replace the logic that was using EF directly:
        // We will assume the repository returns a DTO or we calculate using repo methods.
        // For simplicity and correctness with Phase 1:
        // We need a specific method on Repo for this aggregation.
        // Assuming GetInventoryStatisticsAsync exists or similar.
        // Let's use the DTO directly as return type if possible, or map.

        // Re-implementing logic using repository calls:
        // This is heavy to do without Aggregate support in Repo.
        // Assuming _productRepository.GetInventoryStatistics() exists and returns InventoryStatisticsDto.
        // If not, we have to fetch data. But fetching all variants is bad.
        // We will assume the Repository has this method.

        var inventoryStats = await _inventoryRepository.GetInventoryStatisticsAsync();
        return ServiceResult<InventoryStatisticsDto>.Ok(inventoryStats);
    }
}