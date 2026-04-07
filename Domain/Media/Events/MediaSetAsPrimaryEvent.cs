using Domain.Media.ValueObjects;

namespace Domain.Media.Events;

public sealed class MediaSetAsPrimaryEvent(MediaId mediaId, string entityType, int entityId) : DomainEvent
{
    public MediaId MediaId { get; } = mediaId;
    public string EntityType { get; } = entityType;
    public int EntityId { get; } = entityId;
}