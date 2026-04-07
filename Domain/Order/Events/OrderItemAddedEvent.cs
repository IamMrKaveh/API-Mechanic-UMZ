using Domain.Order.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Order.Events;

public sealed class OrderItemAddedEvent(OrderId orderId, VariantId variantId, int quantity, decimal unitPrice) : DomainEvent
{
    public OrderId OrderId { get; } = orderId;
    public VariantId VariantId { get; } = variantId;
    public int Quantity { get; } = quantity;
    public decimal UnitPrice { get; } = unitPrice;
}