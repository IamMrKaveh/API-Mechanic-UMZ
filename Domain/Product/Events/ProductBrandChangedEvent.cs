using Domain.Brand.ValueObjects;
using Domain.Product.ValueObjects;

namespace Domain.Product.Events;

public sealed class ProductBrandChangedEvent(
    ProductId ProductId,
    BrandId PreviousBrandId,
    BrandId NewBrandId) : DomainEvent
{
    public ProductId ProductId { get; } = ProductId;
    public BrandId PreviousBrandId { get; } = PreviousBrandId;
    public BrandId NewBrandId { get; } = NewBrandId;
}