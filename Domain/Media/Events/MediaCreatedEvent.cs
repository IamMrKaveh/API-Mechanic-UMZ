namespace Domain.Media.Events;

public sealed class MediaCreatedEvent(Guid mediaId, string entityType, int entityId) : DomainEvent
{
    public Guid MediaId { get; } = mediaId;
    public string EntityType { get; } = entityType;
    public int EntityId { get; } = entityId;
}