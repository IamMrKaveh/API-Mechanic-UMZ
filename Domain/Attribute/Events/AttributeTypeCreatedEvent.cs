using Domain.Attribute.ValueObjects;

namespace Domain.Attribute.Events;

public sealed record AttributeTypeCreatedEvent(
    AttributeTypeId AttributeTypeId,
    string Name,
    string DisplayName,
    int SortOrder) : DomainEvent;
