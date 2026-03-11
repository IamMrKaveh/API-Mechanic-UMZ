namespace Domain.Inventory.Events;

public class OutOfStockEvent(int variantId, int productId, string productName) : DomainEvent
{
    public int VariantId { get; } = variantId;
    public int ProductId { get; } = productId;
    public string ProductName { get; } = productName;
    public DateTime OutOfStockAt { get; } = DateTime.UtcNow;
}