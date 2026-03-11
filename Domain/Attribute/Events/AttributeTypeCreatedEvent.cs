using Domain.Attribute.ValueObjects;
using Domain.Common.Abstractions;

namespace Domain.Attribute.Events;

public sealed record AttributeTypeCreatedEvent(
    AttributeTypeId AttributeTypeId,
    string Name,
    string DisplayName,
    int SortOrder) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}