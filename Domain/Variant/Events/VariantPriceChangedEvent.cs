using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Events;

public sealed class ProductVariantPriceChangedEvent(
    VariantId variantId,
    ProductId productId,
    Money previousPrice,
    Money newPrice) : DomainEvent
{
    public VariantId VariantId { get; } = variantId;
    public ProductId ProductId { get; } = productId;
    public Money PreviousPrice { get; } = previousPrice;
    public Money NewPrice { get; } = newPrice;
}