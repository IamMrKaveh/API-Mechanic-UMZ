using Domain.Category.ValueObjects;

namespace Domain.Category.Events;

public sealed class CategoryActivatedEvent(CategoryId categoryId) : DomainEvent
{
    public CategoryId CategoryId { get; } = categoryId;
}