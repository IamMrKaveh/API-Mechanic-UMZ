namespace Domain.Categories.Events;

public class CategoryGroupDeletedEvent : DomainEvent
{
    public int GroupId { get; }
    public int CategoryId { get; }

    public CategoryGroupDeletedEvent(int groupId, int categoryId)
    {
        GroupId = groupId;
        CategoryId = categoryId;
    }
}