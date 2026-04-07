using Domain.Category.ValueObjects;

namespace Domain.Category.Events;

public sealed class CategoriesReorderedEvent(IReadOnlyList<CategoryId> categoryIds) : DomainEvent
{
    public IReadOnlyList<CategoryId> CategoryIds { get; } = categoryIds;
}