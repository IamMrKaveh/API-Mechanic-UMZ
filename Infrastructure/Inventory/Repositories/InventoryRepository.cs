using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Inventory.Repositories;

public sealed class InventoryRepository(DBContext context) : IInventoryRepository
{
    public async Task<Domain.Inventory.Aggregates.Inventory?> GetByIdAsync(InventoryId id, CancellationToken ct = default)
        => await context.Inventories
            .Include(i => i.LedgerEntries)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<Domain.Inventory.Aggregates.Inventory?> GetByVariantIdAsync(VariantId variantId, CancellationToken ct = default)
        => await context.Inventories
            .Include(i => i.LedgerEntries)
            .FirstOrDefaultAsync(i => i.VariantId == variantId, ct);

    public async Task<IReadOnlyList<Domain.Inventory.Aggregates.Inventory>> GetByVariantIdsAsync(
        IEnumerable<VariantId> variantIds, CancellationToken ct = default)
    {
        var ids = variantIds.Select(v => v.Value).ToList();
        var results = await context.Inventories
            .Include(i => i.LedgerEntries)
            .Where(i => ids.Contains(i.VariantId.Value))
            .ToListAsync(ct);
        return results.AsReadOnly();
    }

    public async Task AddAsync(Domain.Inventory.Aggregates.Inventory inventory, CancellationToken ct = default)
        => await context.Inventories.AddAsync(inventory, ct);

    public void Update(Domain.Inventory.Aggregates.Inventory inventory)
        => context.Inventories.Update(inventory);

    public void SetOriginalRowVersion(Domain.Inventory.Aggregates.Inventory entity, byte[] rowVersion)
        => context.Entry(entity).OriginalValues["RowVersion"] = rowVersion;

    public async Task<bool> ExistsByVariantIdAsync(VariantId variantId, CancellationToken ct = default)
        => await context.Inventories.AnyAsync(i => i.VariantId == variantId, ct);
}