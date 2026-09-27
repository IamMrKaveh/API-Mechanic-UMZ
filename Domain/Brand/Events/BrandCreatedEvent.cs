using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Domain.Brand.Events;

public sealed record BrandCreatedEvent(BrandId BrandId, BrandName Name, BrandSlug Slug, CategoryId CategoryId) : DomainEvent;
