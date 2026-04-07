using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Interfaces;

public interface IInventoryRepository
{
    Task<Aggregates.Inventory?> GetByIdAsync(InventoryId id, CancellationToken ct = default);

    Task<Aggregates.Inventory?> GetByVariantIdAsync(
        VariantId variantId, CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Inventory>> GetByVariantIdsAsync(
        IEnumerable<VariantId> variantIds, CancellationToken ct = default);

    Task AddAsync(Aggregates.Inventory inventory, CancellationToken ct = default);

    void Update(Aggregates.Inventory inventory);

    void SetOriginalRowVersion(Aggregates.Inventory entity, byte[] rowVersion);

    Task<bool> ExistsByVariantIdAsync(
        VariantId variantId, CancellationToken ct = default);
}