using Domain.Category.ValueObjects;

namespace Domain.Category.Events;

public sealed class CategoryParentChangedEvent : DomainEvent
{
    public CategoryId CategoryId { get; }
    public CategoryId? PreviousParentId { get; }
    public CategoryId? NewParentId { get; }

    public CategoryParentChangedEvent(CategoryId categoryId, CategoryId? previousParentId, CategoryId? newParentId)
    {
        CategoryId = categoryId;
        PreviousParentId = previousParentId;
        NewParentId = newParentId;
    }
}