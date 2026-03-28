using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed class StockCommittedEvent(
    InventoryId inventoryId,
    ProductVariantId variantId,
    int orderItemId,
    int quantity) : DomainEvent
{
    public InventoryId InventoryId { get; } = inventoryId;
    public ProductVariantId VariantId { get; } = variantId;
    public int OrderItemId { get; } = orderItemId;
    public int Quantity { get; } = quantity;
}