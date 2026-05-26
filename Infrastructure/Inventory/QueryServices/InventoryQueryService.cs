using Application.Inventory.Features.Shared;
using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Inventory.QueryServices;

public sealed class InventoryQueryService(DBContext context) : IInventoryQueryService
{
    public async Task<InventoryDto?> GetByVariantIdAsync(VariantId variantId, CancellationToken ct = default)
    {
        var inventory = await context.Inventories
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.VariantId == variantId, ct);
        return inventory?.Adapt<InventoryDto>();
    }

    public async Task<PaginatedResult<InventoryDto>> GetLowStockAsync(
        StockQuantity threshold, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.Inventories
            .AsNoTracking()
            .Where(i => !i.IsUnlimited && i.AvailableQuantity <= threshold.Value && i.AvailableQuantity > 0);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(i => i.AvailableQuantity)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return PaginatedResult<InventoryDto>.Create(items.Adapt<List<InventoryDto>>(), total, page, pageSize);
    }

    public async Task<PaginatedResult<InventoryDto>> GetOutOfStockAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.Inventories
            .AsNoTracking()
            .Where(i => !i.IsUnlimited && i.AvailableQuantity <= 0);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(i => i.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return PaginatedResult<InventoryDto>.Create(items.Adapt<List<InventoryDto>>(), total, page, pageSize);
    }

    public async Task<IReadOnlyList<InventoryDto>> GetByVariantIdsAsync(
        IEnumerable<VariantId> variantIds, CancellationToken ct = default)
    {
        var ids = variantIds.Select(v => v.Value).ToList();
        var items = await context.Inventories
            .AsNoTracking()
            .Where(i => ids.Contains(i.VariantId.Value))
            .ToListAsync(ct);
        return items.Adapt<List<InventoryDto>>().AsReadOnly();
    }

    public async Task<IReadOnlyList<VariantAvailabilityDto>> GetBatchAvailabilityAsync(
        ICollection<VariantId> variantIds, CancellationToken ct = default)
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
                VariantId = i.VariantId.Value,
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
                VariantId = i.VariantId.Value
            })
            .ToListAsync(ct);
        return result;
    }

    public async Task<InventoryStatisticsDto?> GetStatisticsAsync(CancellationToken ct = default)
    {
        var inventories = await context.Inventories
            .AsNoTracking()
            .ToListAsync(ct);

        if (!inventories.Any()) return null;

        return new InventoryStatisticsDto
        {
            TotalVariants = inventories.Count,
            UnlimitedVariants = inventories.Count(i => i.IsUnlimited),
            InStockVariants = inventories.Count(i => i.IsInStock),
            OutOfStockVariants = inventories.Count(i => i.IsOutOfStock),
            LowStockVariants = inventories.Count(i => i.IsLowStock)
        };
    }

    public async Task<InventoryStatusDto?> GetInventoryStatusAsync(VariantId variantId, CancellationToken ct = default)
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
    VariantId variantId, CancellationToken ct = default)
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
}