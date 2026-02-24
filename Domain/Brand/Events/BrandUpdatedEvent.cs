namespace Domain.Brand.Events;

public class BrandUpdatedEvent : DomainEvent
{
    public int GroupId { get; }
    public string NewName { get; }

    public BrandUpdatedEvent(int groupId, string newName)
    {
        GroupId = groupId;
        NewName = newName;
    }
}