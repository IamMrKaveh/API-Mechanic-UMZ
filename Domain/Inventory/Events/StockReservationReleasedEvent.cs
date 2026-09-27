using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed record StockReservationReleasedEvent(
    InventoryId InventoryId,
    VariantId VariantId,
    int QuantityReleased,
    int TotalReservedQuantity) : DomainEvent;
