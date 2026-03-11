namespace Domain.Category.Events;

public sealed class CategoriesReorderedEvent : DomainEvent
{
    public IReadOnlyList<CategoryId> CategoryIds { get; }

    public CategoriesReorderedEvent(IReadOnlyList<CategoryId> categoryIds)
    {
        CategoryIds = categoryIds;
    }
}