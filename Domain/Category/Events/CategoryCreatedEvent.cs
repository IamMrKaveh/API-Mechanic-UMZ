using Domain.Category.ValueObjects;

namespace Domain.Category.Events;

public sealed class CategoryCreatedEvent : DomainEvent
{
    public CategoryId CategoryId { get; }
    public string Name { get; }
    public string Slug { get; }
    public CategoryId? ParentCategoryId { get; }

    public CategoryCreatedEvent(CategoryId categoryId, string name, string slug, CategoryId? parentCategoryId)
    {
        CategoryId = categoryId;
        Name = name;
        Slug = slug;
        ParentCategoryId = parentCategoryId;
    }
}