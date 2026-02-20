namespace Domain.Category.Events;

public class CategoryDeactivatedEvent : DomainEvent
{
    public int CategoryId { get; }

    public CategoryDeactivatedEvent(int categoryId)
    {
        CategoryId = categoryId;
    }
}