using Domain.Category.ValueObjects;

namespace Domain.Category.Events;

public sealed class CategoryActivatedEvent : DomainEvent
{
    public CategoryId CategoryId { get; }

    public CategoryActivatedEvent(CategoryId categoryId)
    {
        CategoryId = categoryId;
    }
}