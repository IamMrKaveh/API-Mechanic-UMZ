using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;

namespace Domain.Product.Events;

public sealed class ProductCreatedEvent(
ProductId productId,
    ProductName name,
    BrandId brandId,
    CategoryId categoryId) : DomainEvent
{
    public ProductId ProductId { get; } = productId;
    public BrandId BrandId { get; } = brandId;
    public ProductName ProductName { get; } = name;
    public CategoryId CategoryId { get; } = categoryId;
}