using Domain.Common.Abstractions;
using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed record StockReservationReleasedEvent(
    InventoryId InventoryId,
    ProductVariantId VariantId,
    int QuantityReleased,
    int TotalReservedQuantity) : IDomainEvent;