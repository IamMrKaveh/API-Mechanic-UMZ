using Domain.Common.Abstractions;
using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed record StockSetUnlimitedEvent(
    InventoryId InventoryId,
    ProductVariantId VariantId) : IDomainEvent;