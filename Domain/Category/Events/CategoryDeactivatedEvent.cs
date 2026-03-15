using Domain.Category.ValueObjects;

namespace Domain.Category.Events;

public sealed class CategoryDeactivatedEvent : DomainEvent
{
    public CategoryId CategoryId { get; }

    public CategoryDeactivatedEvent(CategoryId categoryId)
    {
        CategoryId = categoryId;
    }
}