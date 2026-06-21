using Application.Inventory.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;
using Mapster;

namespace Infrastructure.Inventory.QueryServices;

public sealed class InventoryQueryService(DBContext context) : IInventoryQueryService
{
    public async Task<InventoryDto?> GetByVariantIdAsync(
        VariantId variantId,
        CancellationToken ct = default)
    {
        var inventory = await context.Inventories
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.VariantId == variantId, ct);
        return inventory?.Adapt<InventoryDto>();
    }

    public async Task<IReadOnlyList<VariantAvailabilityDto>> GetBatchAvailabilityAsync(
        ICollection<VariantId> variantIds,
        CancellationToken ct = default)
    {
        var ids = variantIds.Select(v => v.Value).ToList();
        var inventories = await context.Inventories
            .AsNoTracking()
            .Where(i => ids.Contains(i.VariantId.Value))
            .ToListAsync(ct);

        return inventories.Select(i => new VariantAvailabilityDto
        {
            VariantId = i.VariantId.Value,
            IsAvailable = i.IsInStock,
            AvailableQuantity = i.AvailableQuantity,
            IsUnlimited = i.IsUnlimited,
            IsLowStock = i.IsLowStock
        }).ToList().AsReadOnly();
    }

    public async Task<PaginatedResult<InventoryTransactionDto>> GetTransactionsPagedAsync(
        VariantId? variantId,
        string? transactionType,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.StockLedgerEntries.AsNoTracking().AsQueryable();

        if (variantId is not null)
            query = query.Where(e => e.VariantId == variantId);
        if (!string.IsNullOrWhiteSpace(transactionType))
            query = query.Where(e => e.EventTypeName == transactionType);
        if (fromDate.HasValue)
            query = query.Where(e => e.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(e => e.CreatedAt <= toDate.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new InventoryTransactionDto
            {
                Id = e.Id.Value,
                VariantId = e.VariantId.Value,
                TransactionType = e.EventTypeName,
                QuantityChange = e.QuantityDelta,
                BalanceAfter = e.BalanceAfter,
                ReferenceNumber = e.ReferenceNumber,
                Notes = e.Note,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync(ct);

        return PaginatedResult<InventoryTransactionDto>.Create(items, total, page, pageSize);
    }

    public async Task<IEnumerable<LowStockItemDto>> GetLowStockProductsAsync(
        int threshold, CancellationToken ct = default)
    {
        var result = await context.Inventories
            .AsNoTracking()
            .Where(i => !i.IsUnlimited && i.AvailableQuantity > 0 && i.AvailableQuantity <= threshold)
            .Select(i => new LowStockItemDto
            {
                ProductId = i.Variant.ProductId.Value,
                VariantId = i.VariantId.Value,
                ProductName = i.Variant.Product.Name.Value,
                Sku = i.Variant.Sku.Value,
                StockQuantity = i.StockQuantity.Value,
                LowStockThreshold = i.LowStockThreshold
            })
            .OrderBy(i => i.StockQuantity)
            .ToListAsync(ct);
        return result;
    }

    public async Task<IEnumerable<OutOfStockItemDto>> GetOutOfStockProductsAsync(CancellationToken ct = default)
    {
        var result = await context.Inventories
            .AsNoTracking()
            .Where(i => !i.IsUnlimited && i.AvailableQuantity <= 0)
            .Select(i => new OutOfStockItemDto
            {
                VariantId = i.VariantId.Value,
                ProductName = i.Variant.Product.Name.Value,
                Sku = i.Variant.Sku.Value
            })
            .ToListAsync(ct);
        return result;
    }

    public async Task<InventoryStatisticsDto?> GetStatisticsAsync(CancellationToken ct = default)
    {
        var stats = await context.Inventories
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalVariants = g.Count(),
                UnlimitedVariants = g.Count(i => i.IsUnlimited),
                InStockVariants = g.Count(i => i.IsInStock),
                OutOfStockVariants = g.Count(i => i.IsOutOfStock),
                LowStockVariants = g.Count(i => i.IsLowStock)
            })
            .FirstOrDefaultAsync(ct);

        if (stats is null || stats.TotalVariants == 0) return null;

        return new InventoryStatisticsDto
        {
            TotalVariants = stats.TotalVariants,
            UnlimitedVariants = stats.UnlimitedVariants,
            InStockVariants = stats.InStockVariants,
            OutOfStockVariants = stats.OutOfStockVariants,
            LowStockVariants = stats.LowStockVariants
        };
    }

    public async Task<InventoryStatusDto?> GetInventoryStatusAsync(
        VariantId variantId,
        CancellationToken ct = default)
    {
        var inventory = await context.Inventories
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.VariantId == variantId, ct);

        if (inventory is null) return null;

        return new InventoryStatusDto
        {
            VariantId = inventory.VariantId.Value,
            StockQuantity = inventory.StockQuantity.Value,
            ReservedQuantity = inventory.ReservedQuantity.Value,
            AvailableStock = inventory.AvailableQuantity,
            IsInStock = inventory.IsInStock,
            IsUnlimited = inventory.IsUnlimited,
            IsLowStock = inventory.IsLowStock
        };
    }

    public async Task<IEnumerable<WarehouseStockDto>> GetWarehouseStockByVariantAsync(
        VariantId variantId,
        CancellationToken ct = default)
    {
        var inventory = await context.Inventories
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.VariantId == variantId, ct);

        if (inventory is null)
            return [];

        var ledgerByWarehouse = await context.StockLedgerEntries
            .AsNoTracking()
            .Where(e => e.VariantId == variantId && e.WarehouseId != null)
            .GroupBy(e => e.WarehouseId)
            .Select(g => new
            {
                WarehouseId = g.Key,
                NetQuantity = g.Sum(e => e.QuantityDelta)
            })
            .Where(x => x.NetQuantity > 0)
            .ToListAsync(ct);

        if (ledgerByWarehouse.Count == 0)
        {
            var defaultWarehouse = await context.Warehouses
                .AsNoTracking()
                .Where(w => w.IsDefault && w.IsActive)
                .FirstOrDefaultAsync(ct);

            return
            [
            new WarehouseStockDto
            {
                WarehouseId = defaultWarehouse?.Id.Value ?? Guid.Empty,
                WarehouseName = defaultWarehouse?.Name,
                VariantId = inventory.VariantId.Value,
                Quantity = inventory.StockQuantity.Value,
                ReservedQuantity = inventory.ReservedQuantity.Value
            }
        ];
        }

        var warehouseGuids = ledgerByWarehouse
            .Select(x => x.WarehouseId!.Value)
            .ToList();

        var warehouses = await context.Warehouses
            .AsNoTracking()
            .Where(w => warehouseGuids.Contains(w.Id.Value))
            .ToDictionaryAsync(w => w.Id.Value, w => w.Name, ct);

        var totalNet = ledgerByWarehouse.Sum(x => x.NetQuantity);

        return ledgerByWarehouse.Select(x => new WarehouseStockDto
        {
            WarehouseId = x.WarehouseId!.Value,
            WarehouseName = warehouses.GetValueOrDefault(x.WarehouseId!.Value),
            VariantId = inventory.VariantId.Value,
            Quantity = Math.Max(0, x.NetQuantity),
            ReservedQuantity = totalNet > 0
                ? (int)Math.Round((double)x.NetQuantity / totalNet * inventory.ReservedQuantity.Value)
                : 0
        });
    }

    public async Task<IReadOnlyList<InventoryStatusDto>> GetInventoryStatusesByProductAsync(
        ProductId productId,
        CancellationToken ct = default)
    {
        var variantIds = await context.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == productId && !v.IsDeleted)
            .Select(v => v.Id)
            .ToListAsync(ct);

        if (variantIds.Count == 0)
            return Array.Empty<InventoryStatusDto>();

        var inventories = await context.Inventories
            .AsNoTracking()
            .Where(i => variantIds.Contains(i.VariantId))
            .ToListAsync(ct);

        return inventories
            .Select(inventory => new InventoryStatusDto
            {
                VariantId = inventory.VariantId.Value,
                StockQuantity = inventory.StockQuantity.Value,
                ReservedQuantity = inventory.ReservedQuantity.Value,
                AvailableStock = inventory.AvailableQuantity,
                IsInStock = inventory.IsInStock,
                IsUnlimited = inventory.IsUnlimited,
                IsLowStock = inventory.IsLowStock
            })
            .ToList();
    }
}