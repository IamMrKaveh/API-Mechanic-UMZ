namespace Domain.Product.Events;

public sealed class PriceChangedEvent : DomainEvent
{
    public int VariantId { get; }
    public int ProductId { get; }
    public decimal OldPrice { get; }
    public decimal NewPrice { get; }
    public decimal? OldOriginalPrice { get; }
    public decimal? NewOriginalPrice { get; }

    public PriceChangedEvent(int variantId, int productId, decimal oldPrice, decimal newPrice, decimal? oldOriginalPrice, decimal? newOriginalPrice)
    {
        VariantId = variantId;
        ProductId = productId;
        OldPrice = oldPrice;
        NewPrice = newPrice;
        OldOriginalPrice = oldOriginalPrice;
        NewOriginalPrice = newOriginalPrice;
    }
}