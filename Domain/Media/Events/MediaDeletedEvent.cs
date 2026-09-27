using Domain.Media.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Media.Events;

public sealed record MediaDeletedEvent(MediaId MediaId, string EntityType, Guid EntityId, UserId? DeletedBy) : DomainEvent;
