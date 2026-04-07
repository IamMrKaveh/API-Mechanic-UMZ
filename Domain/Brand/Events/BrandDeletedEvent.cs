using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Domain.Brand.Events;

public sealed class BrandDeletedEvent(BrandId brandId, CategoryId categoryId) : DomainEvent
{
    public BrandId BrandId { get; } = brandId;
    public CategoryId CategoryId { get; } = categoryId;
}