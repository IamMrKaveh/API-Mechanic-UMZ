namespace Infrastructure.Inventory.QueryServices;

/// <summary>
/// Read-side Query Service - مستقیماً DTO برمی‌گرداند
/// بدون عبور از Domain Model برای خواندن داده
/// </summary>
public class InventoryQueryService : IInventoryQueryService
{
    private readonly LedkaContext _context;
    private readonly IMediaService _mediaService;

    public InventoryQueryService(LedkaContext context, IMediaService mediaService)
    {
        _context = context;
        _mediaService = mediaService;
    }

    public async Task<InventoryStatusDto?> GetInventoryStatusAsync(int variantId, CancellationToken ct = default)
    {
        var variant = await _context.Set<ProductVariant>()
            .Include(v => v.Product)
            .Where(v => v.Id == variantId)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (variant is null)
            return null;

        var lastTransaction = await _context.InventoryTransactions
            .Where(t => t.VariantId == variantId)
            .OrderByDescending(t => t.CreatedAt)
            .Include(t => t.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        InventoryTransactionDto? lastTransactionDto = null;
        if (lastTransaction is not null)
        {
            lastTransactionDto = MapTransactionToDto(lastTransaction, variant.Product?.Name ?? string.Empty);
        }

        var available = variant.IsUnlimited ? int.MaxValue : variant.StockQuantity - variant.ReservedQuantity;

        return new InventoryStatusDto
        {
            VariantId = variant.Id,
            ProductId = variant.ProductId,
            ProductName = variant.Product?.Name ?? string.Empty,
            VariantSku = variant.Sku,
            VariantDisplayName = variant.DisplayName,
            StockQuantity = variant.StockQuantity,
            ReservedQuantity = variant.ReservedQuantity,
            AvailableStock = available,
            IsUnlimited = variant.IsUnlimited,
            IsInStock = variant.IsUnlimited || available > 0,
            IsLowStock = !variant.IsUnlimited && available > 0 && available <= variant.LowStockThreshold,
            LowStockThreshold = variant.LowStockThreshold,
            PurchasePrice = variant.PurchasePrice,
            SellingPrice = variant.SellingPrice,
            InventoryValue = variant.PurchasePrice * variant.StockQuantity,
            LastTransaction = lastTransactionDto
        };
    }

    public async Task<IEnumerable<LowStockItemDto>> GetLowStockProductsAsync(int threshold, CancellationToken ct = default)
    {
        var items = await _context.Set<ProductVariant>()
            .Include(v => v.Product)
                .ThenInclude(p => p!.CategoryGroup)
                    .ThenInclude(cg => cg!.Category)
            .Where(v => v.IsActive && !v.IsDeleted && !v.IsUnlimited)
            .Where(v => v.StockQuantity - v.ReservedQuantity > 0
                     && v.StockQuantity - v.ReservedQuantity <= threshold)
            .OrderBy(v => v.StockQuantity - v.ReservedQuantity)
            .AsNoTracking()
            .ToListAsync(ct);

        return items.Select(v => new LowStockItemDto
        {
            VariantId = v.Id,
            ProductId = v.ProductId,
            ProductName = v.Product?.Name ?? string.Empty,
            Sku = v.Sku,
            Stock = v.StockQuantity,
            AvailableStock = v.StockQuantity - v.ReservedQuantity,
            ReservedQuantity = v.ReservedQuantity,
            CategoryName = v.Product?.CategoryGroup?.Category?.Name?.Value,
            SellingPrice = v.SellingPrice,
            VariantDisplayName = v.DisplayName,
            LowStockThreshold = v.LowStockThreshold
        });
    }

    public async Task<IEnumerable<OutOfStockItemDto>> GetOutOfStockProductsAsync(CancellationToken ct = default)
    {
        var items = await _context.Set<ProductVariant>()
            .Include(v => v.Product)
                .ThenInclude(p => p!.CategoryGroup)
                    .ThenInclude(cg => cg!.Category)
            .Where(v => v.IsActive && !v.IsDeleted && !v.IsUnlimited)
            .Where(v => v.StockQuantity - v.ReservedQuantity <= 0)
            .OrderBy(v => v.Product!.Name)
            .AsNoTracking()
            .ToListAsync(ct);

        // دریافت تاریخ آخرین فروش هر واریانت
        var variantIds = items.Select(v => v.Id).ToList();
        var lastSaleDates = await _context.InventoryTransactions
            .Where(t => variantIds.Contains(t.VariantId) && t.TransactionType == "Sale")
            .GroupBy(t => t.VariantId)
            .Select(g => new { VariantId = g.Key, LastSaleDate = g.Max(t => t.CreatedAt) })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.VariantId, x => x.LastSaleDate, ct);

        return items.Select(v => new OutOfStockItemDto
        {
            VariantId = v.Id,
            ProductId = v.ProductId,
            ProductName = v.Product?.Name ?? string.Empty,
            Sku = v.Sku,
            CategoryName = v.Product?.CategoryGroup?.Category?.Name?.Value,
            SellingPrice = v.SellingPrice,
            VariantDisplayName = v.DisplayName,
            LastSaleDate = lastSaleDates.GetValueOrDefault(v.Id)
        });
    }

    public async Task<PaginatedResult<InventoryTransactionDto>> GetTransactionsAsync(
        int? variantId,
        string? transactionType,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.InventoryTransactions
            .Include(t => t.Variant)
                .ThenInclude(v => v!.Product)
            .Include(t => t.User)
            .AsQueryable();

        if (variantId.HasValue)
            query = query.Where(t => t.VariantId == variantId.Value);

        if (!string.IsNullOrWhiteSpace(transactionType))
            query = query.Where(t => t.TransactionType == transactionType);

        if (fromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(t => t.CreatedAt <= toDate.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(ct);

        var dtos = new List<InventoryTransactionDto>();
        foreach (var t in items)
        {
            var dto = MapTransactionToDto(t, t.Variant?.Product?.Name ?? string.Empty);

            // دریافت تصویر محصول
            if (t.Variant?.ProductId > 0)
            {
                var mediaUrl = await _mediaService.GetPrimaryImageUrlAsync("Product", t.Variant.ProductId);
                dto.ProductImage = mediaUrl;
            }

            dtos.Add(dto);
        }

        return PaginatedResult<InventoryTransactionDto>.Create(dtos, totalCount, page, pageSize);
    }

    public async Task<InventoryStatisticsDto> GetStatisticsAsync(CancellationToken ct = default)
    {
        var variants = await _context.Set<ProductVariant>()
            .Where(v => v.IsActive && !v.IsDeleted)
            .AsNoTracking()
            .ToListAsync(ct);

        var totalVariants = variants.Count;
        var unlimitedVariants = variants.Count(v => v.IsUnlimited);
        var finiteVariants = variants.Where(v => !v.IsUnlimited).ToList();

        var outOfStock = finiteVariants.Count(v => v.StockQuantity - v.ReservedQuantity <= 0);
        var lowStock = finiteVariants.Count(v =>
        {
            var available = v.StockQuantity - v.ReservedQuantity;
            return available > 0 && available <= v.LowStockThreshold;
        });
        var inStock = finiteVariants.Count(v => v.StockQuantity - v.ReservedQuantity > 0);

        var totalInventoryValue = finiteVariants.Sum(v => v.PurchasePrice * v.StockQuantity);
        var totalSellingValue = finiteVariants.Sum(v => v.SellingPrice * v.StockQuantity);
        var potentialProfit = totalSellingValue - totalInventoryValue;

        var inStockPercentage = totalVariants > 0
            ? Math.Round((decimal)inStock / totalVariants * 100, 2) : 0;
        var outOfStockPercentage = totalVariants > 0
            ? Math.Round((decimal)outOfStock / totalVariants * 100, 2) : 0;

        return new InventoryStatisticsDto
        {
            TotalVariants = totalVariants,
            InStockVariants = inStock,
            LowStockVariants = lowStock,
            OutOfStockVariants = outOfStock,
            UnlimitedVariants = unlimitedVariants,
            TotalInventoryValue = totalInventoryValue,
            TotalSellingValue = totalSellingValue,
            PotentialProfit = potentialProfit,
            InStockPercentage = inStockPercentage,
            OutOfStockPercentage = outOfStockPercentage
        };
    }

    private static InventoryTransactionDto MapTransactionToDto(InventoryTransaction t, string productName)
    {
        return new InventoryTransactionDto
        {
            Id = t.Id,
            VariantId = t.VariantId,
            ProductId = t.Variant?.ProductId ?? 0,
            ProductName = productName,
            VariantSku = t.Variant?.Sku,
            VariantName = t.Variant?.DisplayName,
            TransactionType = t.TransactionType,
            QuantityChange = t.QuantityChange,
            StockBefore = t.StockBefore,
            StockAfter = t.StockBefore + t.QuantityChange,
            Notes = t.Notes,
            ReferenceNumber = t.ReferenceNumber,
            OrderItemId = t.OrderItemId,
            UserId = t.UserId,
            UserName = t.User != null ? $"{t.User.FirstName} {t.User.LastName}".Trim() : "System",
            CreatedAt = t.CreatedAt
        };
    }
}