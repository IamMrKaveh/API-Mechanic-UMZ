using Domain.Common.Abstractions;
using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed record StockIncreasedEvent(
    InventoryId InventoryId,
    ProductVariantId VariantId,
    int QuantityAdded,
    int NewStockQuantity,
    string Reason = "") : IDomainEvent;