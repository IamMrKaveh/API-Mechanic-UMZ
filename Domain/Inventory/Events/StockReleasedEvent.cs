namespace Domain.Inventory.Events;

public class StockReleasedEvent : DomainEvent
{
    public int VariantId { get; }
    public int ProductId { get; }
    public int Quantity { get; }
    public string? Reason { get; }

    public StockReleasedEvent(int variantId, int productId, int quantity, string? reason = null)
    {
        VariantId = variantId;
        ProductId = productId;
        Quantity = quantity;
        Reason = reason;
    }
}