using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Interfaces;

public interface IInventoryRepository
{
    Task<Aggregates.Inventory?> GetByIdAsync(
        InventoryId id,
        CancellationToken ct = default);

    Task<Aggregates.Inventory?> GetByVariantIdAsync(
        VariantId variantId,
        CancellationToken ct = default);

    Task<Aggregates.Inventory?> GetByVariantIdWithLedgerAsync(
        VariantId variantId,
        CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Inventory>> GetByVariantIdsAsync(
        IEnumerable<VariantId> variantIds,
        CancellationToken ct = default);

    void Update(Aggregates.Inventory inventory);
}