namespace Domain.Product.Events;

public sealed class StockChangedEvent : DomainEvent
{
    public int VariantId { get; }
    public int ProductId { get; }
    public int OldStock { get; }
    public int NewStock { get; }
    public int QuantityChange { get; }
    public string Reason { get; }

    public StockChangedEvent(int variantId, int productId, int oldStock, int newStock, int quantityChange, string reason)
    {
        VariantId = variantId;
        ProductId = productId;
        OldStock = oldStock;
        NewStock = newStock;
        QuantityChange = quantityChange;
        Reason = reason;
    }
}