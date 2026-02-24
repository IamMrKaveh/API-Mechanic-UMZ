namespace Domain.Inventory.Events;

public sealed class StockReturnedEvent : DomainEvent
{
    public int VariantId { get; }
    public int OrderId { get; }
    public int Quantity { get; }

    public StockReturnedEvent(int variantId, int orderId, int quantity)
    {
        VariantId = variantId;
        OrderId = orderId;
        Quantity = quantity;
    }
}