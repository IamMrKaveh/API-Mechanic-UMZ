namespace Domain.Inventory.Events;

public sealed class StockCommittedEvent : DomainEvent
{
    public int VariantId { get; }
    public int OrderId { get; }
    public int Quantity { get; }

    public StockCommittedEvent(int variantId, int orderId, int quantity)
    {
        VariantId = variantId;
        OrderId = orderId;
        Quantity = quantity;
    }
}