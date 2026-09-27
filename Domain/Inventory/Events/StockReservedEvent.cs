using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed record StockReservedEvent(
    InventoryId InventoryId,
    VariantId VariantId,
    int QuantityReserved,
    int TotalReservedQuantity) : DomainEvent;
