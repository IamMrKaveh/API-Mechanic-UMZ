using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;

namespace Domain.Product.Events;

public sealed class ProductCategoryChangedEvent(
    ProductId ProductId,
    CategoryId PreviousCategoryId,
    CategoryId NewCategoryId) : DomainEvent
{
    public ProductId ProductId { get; } = ProductId;
    public CategoryId PreviousCategoryId { get; } = PreviousCategoryId;
    public CategoryId NewCategoryId { get; } = NewCategoryId;
}