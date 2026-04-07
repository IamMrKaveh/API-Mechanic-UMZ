using Domain.Category.ValueObjects;

namespace Domain.Category.Events;

public sealed class CategoryDeactivatedEvent(CategoryId categoryId) : DomainEvent
{
    public CategoryId CategoryId { get; } = categoryId;
}