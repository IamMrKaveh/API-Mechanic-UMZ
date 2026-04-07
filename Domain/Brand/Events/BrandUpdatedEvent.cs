using Domain.Brand.ValueObjects;

namespace Domain.Brand.Events;

public sealed class BrandUpdatedEvent(BrandId brandId, BrandName name, Slug slug, string? description) : DomainEvent
{
    public BrandId BrandId { get; } = brandId;
    public BrandName Name { get; } = name;
    public Slug Slug { get; } = slug;
    public string? Description { get; } = description;
}