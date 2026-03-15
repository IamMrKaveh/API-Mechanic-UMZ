using Domain.Common.Abstractions;
using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed class StockReservationReleasedEvent(
    InventoryId InventoryId,
    ProductVariantId VariantId,
    int QuantityReleased,
    int TotalReservedQuantity) : DomainEvent
{
    public InventoryId InventoryId { get; } = InventoryId;
    public ProductVariantId VariantId { get; } = VariantId;
    public int QuantityReleased { get; } = QuantityReleased;
    public int TotalReservedQuantity { get; } = TotalReservedQuantity;
}