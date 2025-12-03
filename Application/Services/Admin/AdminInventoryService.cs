namespace Application.Services.Admin;

public class AdminInventoryService : IAdminInventoryService
{
    private readonly LedkaContext _context;
    private readonly IInventoryService _inventoryService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminInventoryService> _logger;

    public AdminInventoryService(
        LedkaContext context,
        IInventoryService inventoryService,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        ILogger<AdminInventoryService> logger)
    {
        _context = context;
        _inventoryService = inventoryService;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<PagedResultDto<InventoryTransactionDto>>> GetTransactionsAsync(
        int? variantId,
        string? transactionType,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize)
    {
        var query = _context.InventoryTransactions
            .Include(t => t.Variant)
                .ThenInclude(v => v.Product)
            .Include(t => t.User)
            .AsQueryable();

        if (variantId.HasValue)
            query = query.Where(t => t.VariantId == variantId.Value);

        if (!string.IsNullOrEmpty(transactionType))
            query = query.Where(t => t.TransactionType == transactionType);

        if (fromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(t => t.CreatedAt <= toDate.Value);

        var totalItems = await query.CountAsync();

        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new InventoryTransactionDto
            {
                Id = t.Id,
                VariantId = t.VariantId,
                ProductName = t.Variant.Product.Name,
                VariantSku = t.Variant.Sku,
                TransactionType = t.TransactionType,
                QuantityChange = t.QuantityChange,
                StockBefore = t.StockBefore,
                StockAfter = t.StockBefore + t.QuantityChange,
                Notes = t.Notes,
                ReferenceNumber = t.ReferenceNumber,
                OrderItemId = t.OrderItemId,
                UserId = t.UserId,
                UserName = t.User != null ? t.User.FirstName + " " + t.User.LastName : null,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        var result = PagedResultDto<InventoryTransactionDto>.Create(transactions, totalItems, page, pageSize);
        return ServiceResult<PagedResultDto<InventoryTransactionDto>>.Ok(result);
    }

    public async Task<ServiceResult<IEnumerable<LowStockItemDto>>> GetLowStockItemsAsync(int threshold)
    {
        var lowStockItems = await _context.ProductVariants
            .Include(v => v.Product)
                .ThenInclude(p => p.CategoryGroup)
                    .ThenInclude(cg => cg.Category)
            .Include(v => v.InventoryTransactions)
            .Include(v => v.VariantAttributes)
                .ThenInclude(va => va.AttributeValue)
            .Where(v => !v.IsDeleted && v.IsActive && !v.IsUnlimited)
            .ToListAsync();

        var result = lowStockItems
            .Where(v => v.Stock > 0 && v.Stock <= threshold)
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
        var outOfStockItems = await _context.ProductVariants
            .Include(v => v.Product)
                .ThenInclude(p => p.CategoryGroup)
                    .ThenInclude(cg => cg.Category)
            .Include(v => v.InventoryTransactions)
            .Include(v => v.VariantAttributes)
                .ThenInclude(va => va.AttributeValue)
            .Where(v => !v.IsDeleted && v.IsActive && !v.IsUnlimited)
            .ToListAsync();

        var variantIds = outOfStockItems.Where(v => v.Stock == 0).Select(v => v.Id).ToList();

        var lastSales = await _context.OrderItems
            .Where(oi => variantIds.Contains(oi.VariantId) && oi.Order.IsPaid)
            .GroupBy(oi => oi.VariantId)
            .Select(g => new { VariantId = g.Key, LastSaleDate = g.Max(oi => oi.Order.CreatedAt) })
            .ToDictionaryAsync(x => x.VariantId, x => x.LastSaleDate);

        var result = outOfStockItems
            .Where(v => v.Stock == 0)
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
                $"Stock adjusted by {dto.QuantityChange} for variant {dto.VariantId}.  Type: {dto.TransactionType}",
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

    public async Task<ServiceResult<InventoryStatisticsDto>> GetStatisticsAsync()
    {
        var variants = await _context.ProductVariants
            .Where(v => !v.IsDeleted && v.IsActive)
            .Include(v => v.InventoryTransactions)
            .ToListAsync();

        var nonUnlimitedVariants = variants.Where(v => !v.IsUnlimited).ToList();

        var stats = new InventoryStatisticsDto
        {
            TotalVariants = variants.Count,
            InStockVariants = nonUnlimitedVariants.Count(v => v.Stock > 5),
            LowStockVariants = nonUnlimitedVariants.Count(v => v.Stock > 0 && v.Stock <= 5),
            OutOfStockVariants = nonUnlimitedVariants.Count(v => v.Stock == 0),
            TotalInventoryValue = nonUnlimitedVariants.Sum(v => v.PurchasePrice * v.Stock),
            TotalSellingValue = nonUnlimitedVariants.Sum(v => v.SellingPrice * v.Stock)
        };

        return ServiceResult<InventoryStatisticsDto>.Ok(stats);
    }
}