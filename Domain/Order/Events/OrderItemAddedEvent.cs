namespace Domain.Order.Events;

public sealed class OrderItemAddedEvent : DomainEvent
{
    public int OrderId { get; }
    public int VariantId { get; }
    public int Quantity { get; }
    public decimal UnitPrice { get; }

    public OrderItemAddedEvent(int orderId, int variantId, int quantity, decimal unitPrice)
    {
        OrderId = orderId;
        VariantId = variantId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}