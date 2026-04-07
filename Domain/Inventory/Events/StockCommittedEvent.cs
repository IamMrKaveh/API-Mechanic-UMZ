using Domain.Inventory.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed class StockCommittedEvent(
    InventoryId inventoryId,
    ProductVariantId variantId,
    OrderItemId orderItemId,
    int quantity) : DomainEvent
{
    public InventoryId InventoryId { get; } = inventoryId;
    public ProductVariantId VariantId { get; } = variantId;
    public OrderItemId OrderItemId { get; } = orderItemId;
    public int Quantity { get; } = quantity;
}