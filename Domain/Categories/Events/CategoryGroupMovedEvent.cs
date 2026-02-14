namespace Domain.Categories.Events;

public class CategoryGroupMovedEvent : DomainEvent
{
    public int GroupId { get; }
    public int OldCategoryId { get; }
    public int NewCategoryId { get; }

    public CategoryGroupMovedEvent(int groupId, int oldCategoryId, int newCategoryId)
    {
        GroupId = groupId;
        OldCategoryId = oldCategoryId;
        NewCategoryId = newCategoryId;
    }
}