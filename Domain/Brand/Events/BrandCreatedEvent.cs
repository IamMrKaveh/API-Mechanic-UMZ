namespace Domain.Brand.Events;

public class BrandCreatedEvent : DomainEvent
{
    public int GroupId { get; }
    public int CategoryId { get; }
    public string Name { get; }

    public BrandCreatedEvent(int groupId, int categoryId, string name)
    {
        GroupId = groupId;
        CategoryId = categoryId;
        Name = name;
    }
}