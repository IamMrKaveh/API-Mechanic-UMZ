using Domain.Category.ValueObjects;

namespace Domain.Category.Events;

public sealed class CategoryParentChangedEvent(CategoryId categoryId, CategoryId? previousParentId, CategoryId? newParentId) : DomainEvent
{
    public CategoryId CategoryId { get; } = categoryId;
    public CategoryId? PreviousParentId { get; } = previousParentId;
    public CategoryId? NewParentId { get; } = newParentId;
}