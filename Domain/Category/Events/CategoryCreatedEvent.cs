using Domain.Category.ValueObjects;

namespace Domain.Category.Events;

public sealed class CategoryCreatedEvent(CategoryId categoryId, string name, Slug slug) : DomainEvent
{
    public CategoryId CategoryId { get; } = categoryId;
    public string Name { get; } = name;
    public Slug Slug { get; } = slug;
}