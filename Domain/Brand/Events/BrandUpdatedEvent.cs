using Domain.Brand.ValueObjects;

namespace Domain.Brand.Events;

public sealed record BrandUpdatedEvent(BrandId BrandId, BrandName Name, BrandSlug Slug, string? Description) : DomainEvent;
