using Domain.Order.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed class StockReturnedEvent(VariantId variantId, OrderId orderId, int quantity) : DomainEvent
{
    public VariantId VariantId { get; } = variantId;
    public OrderId OrderId { get; } = orderId;
    public int Quantity { get; } = quantity;
}