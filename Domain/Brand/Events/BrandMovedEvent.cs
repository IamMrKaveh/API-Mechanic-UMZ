namespace Domain.Brand.Events;

public class BrandMovedEvent : DomainEvent
{
    public int GroupId { get; }
    public int OldCategoryId { get; }
    public int NewCategoryId { get; }

    public BrandMovedEvent(int groupId, int oldCategoryId, int newCategoryId)
    {
        GroupId = groupId;
        OldCategoryId = oldCategoryId;
        NewCategoryId = newCategoryId;
    }
}