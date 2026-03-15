using Domain.Common.Abstractions;
using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed class StockSetUnlimitedEvent(
    InventoryId InventoryId,
    ProductVariantId VariantId) : DomainEvent
{
    public InventoryId InventoryId { get; } = InventoryId;
    public ProductVariantId VariantId { get; } = VariantId;
}