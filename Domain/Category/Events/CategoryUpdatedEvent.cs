using Domain.Category.ValueObjects;

namespace Domain.Category.Events;

public sealed class CategoryUpdatedEvent(CategoryId categoryId, string name, Slug slug, string? description) : DomainEvent
{
    public CategoryId CategoryId { get; } = categoryId;
    public string Name { get; } = name;
    public Slug Slug { get; } = slug;
    public string? Description { get; } = description;
}