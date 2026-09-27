using Domain.Category.ValueObjects;

namespace Domain.Category.Events;

public sealed record CategoryUpdatedEvent(CategoryId CategoryId, string Name, CategorySlug Slug, string? Description) : DomainEvent;
