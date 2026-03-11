namespace Domain.Inventory.Entities;

public sealed class InventoryTransaction : Entity<InventoryTransactionId>
{
    private InventoryTransaction()
    { }

    public InventoryId InventoryId { get; private set; } = default!;
    public ProductVariantId VariantId { get; private set; } = default!;
    public int QuantityDelta { get; private set; }
    public int StockAfterTransaction { get; private set; }
    public InventoryTransactionType Type { get; private set; }
    public string Reason { get; private set; } = default!;
    public DateTime OccurredAt { get; private set; }

    public static InventoryTransaction Create(
        InventoryTransactionId id,
        InventoryId inventoryId,
        ProductVariantId variantId,
        int quantityDelta,
        int stockAfterTransaction,
        InventoryTransactionType type,
        string reason)
    {
        return new InventoryTransaction
        {
            Id = id,
            InventoryId = inventoryId,
            VariantId = variantId,
            QuantityDelta = quantityDelta,
            StockAfterTransaction = stockAfterTransaction,
            Type = type,
            Reason = reason,
            OccurredAt = DateTime.UtcNow
        };
    }
}