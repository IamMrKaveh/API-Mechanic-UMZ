using Domain.Brand.ValueObjects;
using Domain.Product.ValueObjects;

namespace Domain.Product.Events;

public sealed class ProductCreatedEvent(
    ProductId ProductId,
    ProductName productName,
    BrandId BrandId) : DomainEvent
{
    public ProductId ProductId { get; } = ProductId;
    public ProductName ProductName { get; } = productName;
    public BrandId BrandId { get; } = BrandId;
}