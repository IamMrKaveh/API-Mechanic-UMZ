using Domain.Common.Interfaces;
using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Interfaces;

public interface IInventoryRepository : IRepository<Aggregates.Inventory, InventoryId>
{
    Task<Aggregates.Inventory?> GetByVariantIdAsync(
        VariantId variantId,
        CancellationToken ct = default);

    Task<Aggregates.Inventory?> GetByVariantIdWithLedgerAsync(
        VariantId variantId,
        CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Inventory>> GetByVariantIdsAsync(
        IEnumerable<VariantId> variantIds,
        CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Inventory>> GetByReferenceNumberAsync(
        string referenceNumber,
        CancellationToken ct = default);
}