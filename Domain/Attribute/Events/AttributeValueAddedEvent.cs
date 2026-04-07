using Domain.Attribute.ValueObjects;
using Domain.Common.Events;

namespace Domain.Attribute.Events;

public sealed class AttributeValueAddedEvent(
    AttributeTypeId attributeTypeId,
    AttributeValueId attributeValueId,
    string value,
    string displayValue) : DomainEvent
{
    public AttributeTypeId AttributeTypeId { get; } = attributeTypeId;
    public AttributeValueId AttributeValueId { get; } = attributeValueId;
    public string Value { get; } = value;
    public string DisplayValue { get; } = displayValue;
}