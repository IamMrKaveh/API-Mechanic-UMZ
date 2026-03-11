namespace Domain.Inventory.Events;

public class StockReleasedEvent(int variantId, int productId, int quantity, string? reason = null) : DomainEvent
{
    public int VariantId { get; } = variantId;
    public int ProductId { get; } = productId;
    public int Quantity { get; } = quantity;
    public string? Reason { get; } = reason;
}