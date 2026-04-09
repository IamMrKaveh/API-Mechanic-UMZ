using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed class StockReservedEvent(
    InventoryId InventoryId,
    VariantId VariantId,
    int QuantityReserved,
    int TotalReservedQuantity) : DomainEvent
{
    public InventoryId InventoryId { get; } = InventoryId;
    public VariantId VariantId { get; } = VariantId;
    public int QuantityReserved { get; } = QuantityReserved;
    public int TotalReservedQuantity { get; } = TotalReservedQuantity;
}