using Domain.Common.Abstractions;
using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed record StockReservedEvent(
    InventoryId InventoryId,
    ProductVariantId VariantId,
    int QuantityReserved,
    int TotalReservedQuantity) : IDomainEvent;