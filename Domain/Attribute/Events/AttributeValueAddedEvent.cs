using Domain.Attribute.ValueObjects;
using Domain.Common.Abstractions;

namespace Domain.Attribute.Events;

public sealed record AttributeValueAddedEvent(
    AttributeTypeId AttributeTypeId,
    AttributeValueId AttributeValueId,
    string Value,
    string DisplayValue) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}