namespace Domain.Order.Events;

public sealed class OrderItemAddedEvent(int orderId, int variantId, int quantity, decimal unitPrice) : DomainEvent
{
    public int OrderId { get; } = orderId;
    public int VariantId { get; } = variantId;
    public int Quantity { get; } = quantity;
    public decimal UnitPrice { get; } = unitPrice;
}