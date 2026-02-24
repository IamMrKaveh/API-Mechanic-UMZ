namespace Domain.Attribute.Events;

public sealed class AttributeValueAddedEvent : DomainEvent
{
    public int AttributeTypeId { get; }
    public int AttributeValueId { get; }
    public string Value { get; }

    public AttributeValueAddedEvent(int attributeTypeId, int attributeValueId, string value)
    {
        AttributeTypeId = attributeTypeId;
        AttributeValueId = attributeValueId;
        Value = value;
    }
}