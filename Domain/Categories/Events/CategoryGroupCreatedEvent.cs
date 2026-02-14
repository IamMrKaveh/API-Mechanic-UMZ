namespace Domain.Categories.Events;

public class CategoryGroupCreatedEvent : DomainEvent
{
    public int GroupId { get; }
    public int CategoryId { get; }
    public string Name { get; }

    public CategoryGroupCreatedEvent(int groupId, int categoryId, string name)
    {
        GroupId = groupId;
        CategoryId = categoryId;
        Name = name;
    }
}