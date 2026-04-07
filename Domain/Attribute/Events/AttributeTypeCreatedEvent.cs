using Domain.Attribute.ValueObjects;
using Domain.Common.Events;

namespace Domain.Attribute.Events;

public sealed class AttributeTypeCreatedEvent(
    AttributeTypeId attributeTypeId,
    string name,
    string displayName,
    int sortOrder) : DomainEvent
{
    public AttributeTypeId AttributeTypeId { get; } = attributeTypeId;
    public string Name { get; } = name;
    public string DisplayName { get; } = displayName;
    public int SortOrder { get; } = sortOrder;
}