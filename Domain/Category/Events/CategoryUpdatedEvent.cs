namespace Domain.Category.Events;

public class CategoryUpdatedEvent : DomainEvent
{
    public int CategoryId { get; }
    public string NewName { get; }

    public CategoryUpdatedEvent(int categoryId, string newName)
    {
        CategoryId = categoryId;
        NewName = newName;
    }
}