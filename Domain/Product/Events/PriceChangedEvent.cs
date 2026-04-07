using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Product.Events;

public sealed class PriceChangedEvent(ProductVariantId variantId, ProductId productId, decimal oldPrice, decimal newPrice, decimal? oldOriginalPrice, decimal? newOriginalPrice) : DomainEvent
{
    public ProductVariantId VariantId { get; } = variantId;
    public ProductId ProductId { get; } = productId;
    public decimal OldPrice { get; } = oldPrice;
    public decimal NewPrice { get; } = newPrice;
    public decimal? OldOriginalPrice { get; } = oldOriginalPrice;
    public decimal? NewOriginalPrice { get; } = newOriginalPrice;
}