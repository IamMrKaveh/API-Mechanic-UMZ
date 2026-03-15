using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Interfaces;

public interface IInventoryRepository
{
    Task<Aggregates.Inventory?> GetByIdAsync(InventoryId id, CancellationToken ct = default);

    Task<Aggregates.Inventory?> GetByVariantIdAsync(ProductVariantId variantId, CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Inventory>> GetByVariantIdsAsync(IEnumerable<ProductVariantId> variantIds, CancellationToken ct = default);

    Task AddAsync(Aggregates.Inventory inventory, CancellationToken ct = default);

    void Update(Aggregates.Inventory inventory);

    void SetOriginalRowVersion(Aggregates.Inventory entity, byte[] rowVersion);

    Task<bool> ExistsByVariantIdAsync(ProductVariantId variantId, CancellationToken ct = default);

    Task<int> CalculateStockFromLedgerAsync(ProductVariantId variantId, CancellationToken ct = default);
}