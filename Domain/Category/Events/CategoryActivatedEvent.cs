namespace Domain.Category.Events;

public class CategoryActivatedEvent : DomainEvent
{
    public int CategoryId { get; }

    public CategoryActivatedEvent(int categoryId)
    {
        CategoryId = categoryId;
    }
}