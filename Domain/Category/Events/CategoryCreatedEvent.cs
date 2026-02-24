namespace Domain.Category.Events;

public class CategoryCreatedEvent : DomainEvent
{
    public int CategoryId { get; }
    public string Name { get; }

    public CategoryCreatedEvent(int categoryId, string name)
    {
        CategoryId = categoryId;
        Name = name;
    }
}