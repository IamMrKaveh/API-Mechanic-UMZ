namespace Domain.Product.Events;

public sealed class StockChangedEvent(int variantId, int productId, int oldStock, int newStock, int quantityChange, string reason) : DomainEvent
{
    public int VariantId { get; } = variantId;
    public int ProductId { get; } = productId;
    public int OldStock { get; } = oldStock;
    public int NewStock { get; } = newStock;
    public int QuantityChange { get; } = quantityChange;
    public string Reason { get; } = reason;
}