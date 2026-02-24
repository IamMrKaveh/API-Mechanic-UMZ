namespace Domain.Attribute.Events;

public sealed class AttributeTypeCreatedEvent : DomainEvent
{
    public int AttributeTypeId { get; }
    public string Name { get; }

    public AttributeTypeCreatedEvent(int attributeTypeId, string name)
    {
        AttributeTypeId = attributeTypeId;
        Name = name;
    }
}