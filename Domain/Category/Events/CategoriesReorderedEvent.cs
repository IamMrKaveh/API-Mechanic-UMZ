namespace Domain.Category.Events;

public class CategoriesReorderedEvent : DomainEvent
{
    public IReadOnlyList<int> CategoryIds { get; }

    public CategoriesReorderedEvent(IReadOnlyList<int> categoryIds)
    {
        CategoryIds = categoryIds;
    }
}