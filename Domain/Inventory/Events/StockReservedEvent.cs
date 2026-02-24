namespace Domain.Inventory.Events;

public class StockReservedEvent : DomainEvent
{
    public int VariantId { get; }
    public int ProductId { get; }
    public int Quantity { get; }
    public int? OrderItemId { get; }

    public StockReservedEvent(int variantId, int productId, int quantity, int? orderItemId = null)
    {
        VariantId = variantId;
        ProductId = productId;
        Quantity = quantity;
        OrderItemId = orderItemId;
    }
}