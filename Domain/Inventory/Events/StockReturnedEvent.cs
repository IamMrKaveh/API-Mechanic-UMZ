using Domain.Order.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed class StockReturnedEvent(ProductVariantId variantId, OrderId orderId, int quantity) : DomainEvent
{
    public ProductVariantId VariantId { get; } = variantId;
    public OrderId OrderId { get; } = orderId;
    public int Quantity { get; } = quantity;
}