using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;

namespace Domain.Product.Events;

public sealed class ProductCreatedEvent(
    ProductId ProductId,
    ProductName productName,
    CategoryId CategoryId,
    BrandId BrandId) : DomainEvent
{
    public ProductId ProductId { get; } = ProductId;
    public ProductName ProductName { get; } = productName;
    public CategoryId CategoryId { get; } = CategoryId;
    public BrandId BrandId { get; } = BrandId;
}