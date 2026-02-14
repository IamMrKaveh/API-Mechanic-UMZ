namespace Domain.Categories.Events;

public class CategoryGroupUpdatedEvent : DomainEvent
{
    public int GroupId { get; }
    public string NewName { get; }

    public CategoryGroupUpdatedEvent(int groupId, string newName)
    {
        GroupId = groupId;
        NewName = newName;
    }
}