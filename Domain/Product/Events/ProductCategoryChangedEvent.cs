using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;

namespace Domain.Product.Events;

public sealed class ProductCategoryChangedEvent(
    ProductId productId,
    CategoryId previousCategoryId,
    CategoryId newCategoryId) : DomainEvent
{
    public ProductId ProductId { get; } = productId;
    public CategoryId PreviousCategoryId { get; } = previousCategoryId;
    public CategoryId NewCategoryId { get; } = newCategoryId;
}