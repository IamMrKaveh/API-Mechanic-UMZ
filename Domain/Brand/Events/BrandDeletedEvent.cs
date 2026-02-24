namespace Domain.Brand.Events;

public class BrandDeletedEvent : DomainEvent
{
    public int GroupId { get; }
    public int CategoryId { get; }

    public BrandDeletedEvent(int groupId, int categoryId)
    {
        GroupId = groupId;
        CategoryId = categoryId;
    }
}