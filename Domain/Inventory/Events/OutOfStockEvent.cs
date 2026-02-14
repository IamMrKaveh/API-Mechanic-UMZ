namespace Domain.Inventory.Events;

public class OutOfStockEvent : DomainEvent
{
    public int VariantId { get; }
    public int ProductId { get; }
    public string ProductName { get; }
    public DateTime OutOfStockAt { get; }

    public OutOfStockEvent(int variantId, int productId, string productName)
    {
        VariantId = variantId;
        ProductId = productId;
        ProductName = productName;
        OutOfStockAt = DateTime.UtcNow;
    }
}