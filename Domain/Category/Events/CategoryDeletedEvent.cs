namespace Domain.Category.Events;

public sealed class CategoryDeletedEvent : DomainEvent
{
    public CategoryId CategoryId { get; }

    public CategoryDeletedEvent(CategoryId categoryId)
    {
        CategoryId = categoryId;
    }
}