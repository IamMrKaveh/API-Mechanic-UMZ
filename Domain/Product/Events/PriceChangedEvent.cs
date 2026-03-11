namespace Domain.Product.Events;

public sealed class PriceChangedEvent(int variantId, int productId, decimal oldPrice, decimal newPrice, decimal? oldOriginalPrice, decimal? newOriginalPrice) : DomainEvent
{
    public int VariantId { get; } = variantId;
    public int ProductId { get; } = productId;
    public decimal OldPrice { get; } = oldPrice;
    public decimal NewPrice { get; } = newPrice;
    public decimal? OldOriginalPrice { get; } = oldOriginalPrice;
    public decimal? NewOriginalPrice { get; } = newOriginalPrice;
}