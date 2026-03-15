using Domain.Common.Abstractions;
using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed class StockIncreasedEvent(
    InventoryId InventoryId,
    ProductVariantId VariantId,
    int QuantityAdded,
    int NewStockQuantity,
    string Reason = "") : DomainEvent
{
    public InventoryId InventoryId { get; } = InventoryId;
    public ProductVariantId VariantId { get; } = VariantId;
    public int QuantityAdded { get; } = QuantityAdded;
    public int NewStockQuantity { get; } = NewStockQuantity;
    public string Reason { get; } = Reason;
}