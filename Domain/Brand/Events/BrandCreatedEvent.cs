using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Domain.Brand.Events;

public sealed class BrandCreatedEvent(BrandId brandId, BrandName name, Slug slug, CategoryId categoryId) : DomainEvent
{
    public BrandId BrandId { get; } = brandId;
    public BrandName Name { get; } = name;
    public Slug Slug { get; } = slug;
    public CategoryId CategoryId { get; } = categoryId;
}