using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Domain.Brand.Events;

public sealed class BrandCategoryChangedEvent(BrandId brandId, CategoryId previousCategoryId, CategoryId newCategoryId) : DomainEvent
{
    public BrandId BrandId { get; } = brandId;
    public CategoryId PreviousCategoryId { get; } = previousCategoryId;
    public CategoryId NewCategoryId { get; } = newCategoryId;
}