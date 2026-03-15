using Domain.Common.Abstractions;
using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed class StockReservedEvent(
    InventoryId InventoryId,
    ProductVariantId VariantId,
    int QuantityReserved,
    int TotalReservedQuantity) : DomainEvent
{
    public InventoryId InventoryId { get; } = InventoryId;
    public ProductVariantId VariantId { get; } = VariantId;
    public int QuantityReserved { get; } = QuantityReserved;
    public int TotalReservedQuantity { get; } = TotalReservedQuantity;
}