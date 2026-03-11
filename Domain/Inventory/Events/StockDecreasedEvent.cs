using Domain.Common.Abstractions;
using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed record StockDecreasedEvent(
    InventoryId InventoryId,
    ProductVariantId VariantId,
    int QuantityRemoved,
    int NewStockQuantity,
    string Reason = "") : IDomainEvent;