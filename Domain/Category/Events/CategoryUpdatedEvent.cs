namespace Domain.Category.Events;

public sealed class CategoryUpdatedEvent : DomainEvent
{
    public CategoryId CategoryId { get; }
    public string Name { get; }
    public string Slug { get; }
    public string? Description { get; }

    public CategoryUpdatedEvent(CategoryId categoryId, string name, string slug, string? description)
    {
        CategoryId = categoryId;
        Name = name;
        Slug = slug;
        Description = description;
    }
}