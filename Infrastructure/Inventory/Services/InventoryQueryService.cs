namespace Infrastructure.Inventory.Services;

public class InventoryQueryService : IInventoryQueryService
{
    private readonly Persistence.Context.DBContext _context;

    public InventoryQueryService(
        Persistence.Context.DBContext context
        )
    {
        _context = context;
    }

    public async Task<InventoryStatusDto?> GetInventoryStatusAsync(
        int variantId,
        CancellationToken ct = default
        )
    {
        var status = await GetVariantStatusAsync(variantId, ct);
        return status == null
            ? null
            : new InventoryStatusDto
            {
                VariantId = status.VariantId,
                StockQuantity = status.StockQuantity,
                ReservedQuantity = status.ReservedQuantity,
                AvailableStock = status.AvailableStock,
                IsInStock = status.IsInStock,
                IsUnlimited = status.IsUnlimited
            };
    }

    public async Task<VariantStockStatusDto?> GetVariantStatusAsync(
        int variantId,
        CancellationToken ct = default
        )
    {
        var variant = await _context.ProductVariants
            .Where(v => v.Id == variantId && !v.IsDeleted)
            .Select(v => new
            {
                v.Id,
                v.StockQuantity,
                v.ReservedQuantity,
                v.IsUnlimited,
                v.IsActive
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        if (variant == null) return null;

        var available = variant.IsUnlimited
            ? int.MaxValue
            : Math.Max(0, variant.StockQuantity - variant.ReservedQuantity);

        return new VariantStockStatusDto
        {
            VariantId = variant.Id,
            StockQuantity = variant.StockQuantity,
            ReservedQuantity = variant.ReservedQuantity,
            AvailableStock = available,
            IsInStock = variant.IsUnlimited || available > 0,
            IsUnlimited = variant.IsUnlimited
        };
    }

    public async Task<IEnumerable<LowStockItemDto>> GetLowStockProductsAsync(
        int threshold,
        CancellationToken ct = default
        )
    {
        return await _context.ProductVariants
            .Where(v => v.IsActive && !v.IsDeleted && !v.IsUnlimited &&
                        (v.StockQuantity - v.ReservedQuantity) > 0 &&
                        (v.StockQuantity - v.ReservedQuantity) <= threshold)
            .Select(v => new LowStockItemDto
            {
                VariantId = v.Id,
                ProductId = v.ProductId,
                ProductName = v.Product.Name.Value,
                Sku = v.Sku != null ? v.Sku.Value : null,
                StockQuantity = v.StockQuantity,
                ReservedQuantity = v.ReservedQuantity,
                AvailableStock = v.StockQuantity - v.ReservedQuantity,
                LowStockThreshold = v.LowStockThreshold
            })
            .AsNoTracking()
            .OrderBy(v => v.AvailableStock)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<OutOfStockItemDto>> GetOutOfStockProductsAsync(
        CancellationToken ct = default
        )
    {
        return await _context.ProductVariants
            .Where(v => v.IsActive && !v.IsDeleted && !v.IsUnlimited &&
                        (v.StockQuantity - v.ReservedQuantity) <= 0)
            .Select(v => new OutOfStockItemDto
            {
                VariantId = v.Id,
                ProductId = v.ProductId,
                ProductName = v.Product.Name.Value,
                Sku = v.Sku != null ? v.Sku.Value : null,
                StockQuantity = v.StockQuantity,
                ReservedQuantity = v.ReservedQuantity
            })
            .AsNoTracking()
            .OrderByDescending(v => v.ProductId)
            .ToListAsync(ct);
    }

    public async Task<PaginatedResult<InventoryTransactionDto>> GetTransactionsAsync(
        int? variantId,
        string? transactionType,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken ct = default
        )
    {
        var query = _context.InventoryTransactions
            .AsQueryable();

        if (variantId.HasValue) query = query.Where(t => t.VariantId == variantId.Value);
        if (!string.IsNullOrWhiteSpace(transactionType)) query = query.Where(t => t.TransactionType == transactionType);
        if (fromDate.HasValue) query = query.Where(t => t.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(t => t.CreatedAt <= toDate.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new InventoryTransactionDto
            {
                Id = t.Id,
                VariantId = t.VariantId,
                TransactionType = t.TransactionType,
                QuantityChange = t.QuantityChange,
                StockBefore = t.StockBefore,
                Notes = t.Notes,
                ReferenceNumber = t.ReferenceNumber,
                CorrelationId = t.CorrelationId,
                ExpiresAt = t.ExpiresAt,
                IsReversed = t.IsReversed,
                CreatedAt = t.CreatedAt,
                ProductName = t.Variant != null && t.Variant.Product != null ? t.Variant.Product.Name.Value : string.Empty,
                VariantSku = t.Variant != null && t.Variant.Sku != null ? t.Variant.Sku.Value : string.Empty,
                UserName = t.User != null ? t.User.FirstName + " " + t.User.LastName : string.Empty
            })
            .AsNoTracking()
            .ToListAsync(ct);

        return PaginatedResult<InventoryTransactionDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<InventoryStatisticsDto> GetStatisticsAsync(
        CancellationToken ct = default
        )
    {
        var variants = await _context.ProductVariants
            .Where(v => v.IsActive && !v.IsDeleted)
            .Select(v => new
            {
                v.Id,
                v.StockQuantity,
                v.ReservedQuantity,
                v.IsUnlimited,
                v.LowStockThreshold,
                PurchasePrice = v.PurchasePrice.Amount,
                SellingPrice = v.SellingPrice.Amount
            })
            .AsNoTracking()
            .ToListAsync(ct);

        var finite = variants.Where(v => !v.IsUnlimited).ToList();

        return new InventoryStatisticsDto
        {
            TotalVariants = variants.Count,
            InStockCount = finite.Count(v => (v.StockQuantity - v.ReservedQuantity) > 0),
            LowStockCount = finite.Count(v =>
            {
                var avail = v.StockQuantity - v.ReservedQuantity;
                return avail > 0 && avail <= v.LowStockThreshold;
            }),
            OutOfStockCount = finite.Count(v => (v.StockQuantity - v.ReservedQuantity) <= 0),
            UnlimitedCount = variants.Count(v => v.IsUnlimited),
            TotalInventoryValue = finite.Sum(v => v.PurchasePrice * v.StockQuantity),
            TotalSellingValue = finite.Sum(v => v.SellingPrice * v.StockQuantity)
        };
    }
}