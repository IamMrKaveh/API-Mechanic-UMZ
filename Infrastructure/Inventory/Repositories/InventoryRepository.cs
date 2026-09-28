using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

using Infrastructure.Persistence.Repositories;

namespace Infrastructure.Inventory.Repositories;

public sealed class InventoryRepository(DBContext context)
    : RepositoryBase<Domain.Inventory.Aggregates.Inventory, InventoryId>(context), IInventoryRepository
{
    public async Task<Domain.Inventory.Aggregates.Inventory?> GetByVariantIdAsync(VariantId variantId, CancellationToken ct = default)
        => await Context.Inventories
            .FirstOrDefaultAsync(i => i.VariantId == variantId, ct);

    public async Task<Domain.Inventory.Aggregates.Inventory?> GetByVariantIdWithLedgerAsync(VariantId variantId, CancellationToken ct = default)
        => await Context.Inventories
            .Include(i => i.LedgerEntries)
            .FirstOrDefaultAsync(i => i.VariantId == variantId, ct);

    public async Task<IReadOnlyList<Domain.Inventory.Aggregates.Inventory>> GetByVariantIdsAsync(
        IEnumerable<VariantId> variantIds, CancellationToken ct = default)
    {
        var idList = variantIds?.ToList() ?? new List<VariantId>();
        if (idList.Count == 0)
            return [];

        var results = await Context.Inventories
            .Where(i => idList.Contains(i.VariantId))
            .ToListAsync(ct);
        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<Domain.Inventory.Aggregates.Inventory>> GetByReferenceNumberAsync(
        string referenceNumber, CancellationToken ct = default)
    {
        var results = await Context.Inventories
            .Include(i => i.LedgerEntries)
            .Where(i => i.LedgerEntries.Any(e => e.ReferenceNumber == referenceNumber))
            .ToListAsync(ct);
        return results.AsReadOnly();
    }

}
