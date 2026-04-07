using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Domain.Brand.Events;

public sealed class BrandDeactivatedEvent(BrandId brandId, BrandName name, CategoryId categoryId) : DomainEvent
{
    public BrandId BrandId { get; } = brandId;
    public BrandName Name { get; } = name;
    public CategoryId CategoryId { get; } = categoryId;
}