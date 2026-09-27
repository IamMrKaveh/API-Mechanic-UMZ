using Domain.Media.ValueObjects;

namespace Domain.Media.Events;

public sealed record MediaSetAsPrimaryEvent(MediaId MediaId, string EntityType, Guid EntityId) : DomainEvent;
