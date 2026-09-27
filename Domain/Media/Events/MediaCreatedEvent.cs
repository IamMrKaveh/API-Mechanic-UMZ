using Domain.Media.ValueObjects;

namespace Domain.Media.Events;

public sealed record MediaCreatedEvent(MediaId MediaId, string EntityType, Guid EntityId) : DomainEvent;
