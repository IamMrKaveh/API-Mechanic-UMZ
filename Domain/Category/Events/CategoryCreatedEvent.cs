using Domain.Category.ValueObjects;

namespace Domain.Category.Events;

public sealed class CategoryCreatedEvent(CategoryId categoryId, string name, Slug slug, CategoryId? parentCategoryId) : DomainEvent
{
    public CategoryId CategoryId { get; } = categoryId;
    public string Name { get; } = name;
    public Slug Slug { get; } = slug;
    public CategoryId? ParentCategoryId { get; } = parentCategoryId;
}