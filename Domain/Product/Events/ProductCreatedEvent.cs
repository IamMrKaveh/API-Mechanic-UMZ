using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;

namespace Domain.Product.Events;

public sealed class ProductCreatedEvent(
    ProductId ProductId,
    string Name,
    CategoryId CategoryId,
    BrandId BrandId) : DomainEvent
{
    public ProductId ProductId { get; } = ProductId;
    public string Name { get; } = Name;
    public CategoryId CategoryId { get; } = CategoryId;
    public BrandId BrandId { get; } = BrandId;
}