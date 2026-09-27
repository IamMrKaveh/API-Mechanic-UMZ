using Domain.Category.ValueObjects;

namespace Domain.Category.Events;

public sealed record CategoryCreatedEvent(CategoryId CategoryId, string Name, CategorySlug Slug) : DomainEvent;
