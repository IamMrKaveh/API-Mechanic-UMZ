using Domain.Attribute.ValueObjects;

namespace Domain.Attribute.Events;

public sealed record AttributeValueAddedEvent(
    AttributeTypeId AttributeTypeId,
    AttributeValueId AttributeValueId,
    string Value,
    string DisplayValue) : DomainEvent;
