using Domain.Inventory.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed record StockCommittedEvent(
    InventoryId InventoryId,
    VariantId VariantId,
    OrderItemId OrderItemId,
    int Quantity) : DomainEvent;
