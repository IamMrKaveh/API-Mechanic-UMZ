using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Inventory.Repositories;

public sealed class InventoryRepository(DBContext context) : IInventoryRepository
{
    public async Task<Domain.Inventory.Aggregates.Inventory?> GetByIdAsync(InventoryId id, CancellationToken ct = default)
    {
        return await context.Set<Domain.Inventory.Aggregates.Inventory>()
            .Include(i => i.LedgerEntries)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<Domain.Inventory.Aggregates.Inventory?> GetByVariantIdAsync(VariantId variantId, CancellationToken ct = default)
    {
        return await context.Set<Domain.Inventory.Aggregates.Inventory>()
            .Include(i => i.LedgerEntries)
            .FirstOrDefaultAsync(i => i.VariantId == variantId, ct);
    }

    public async Task<IReadOnlyList<Domain.Inventory.Aggregates.Inventory>> GetByVariantIdsAsync(
        IEnumerable<VariantId> variantIds,
        CancellationToken ct = default)
    {
        var ids = variantIds.Select(v => v.Value).ToList();
        var results = await context.Set<Domain.Inventory.Aggregates.Inventory>()
            .Include(i => i.LedgerEntries)
            .Where(i => ids.Contains(i.VariantId.Value))
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task<bool> ExistsByVariantIdAsync(VariantId variantId, CancellationToken ct = default)
    {
        return await context.Set<Domain.Inventory.Aggregates.Inventory>()
            .AnyAsync(i => i.VariantId == variantId, ct);
    }

    public async Task AddAsync(Domain.Inventory.Aggregates.Inventory inventory, CancellationToken ct = default)
    {
        await context.Set<Domain.Inventory.Aggregates.Inventory>().AddAsync(inventory, ct);
    }

    public void Update(Domain.Inventory.Aggregates.Inventory inventory)
    {
        context.Set<Domain.Inventory.Aggregates.Inventory>().Update(inventory);
    }

    public async Task<IReadOnlyList<Domain.Inventory.Aggregates.Inventory>> GetLowStockAsync(CancellationToken ct = default)
    {
        var results = await context.Set<Domain.Inventory.Aggregates.Inventory>()
            .Where(i => !i.IsUnlimited && i.AvailableStock.Value <= i.LowStockThreshold)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }
}