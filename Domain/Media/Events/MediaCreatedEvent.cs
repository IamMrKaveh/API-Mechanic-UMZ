namespace Domain.Media.Events;

public sealed class MediaCreatedEvent : DomainEvent
{
    public int MediaId { get; }
    public string EntityType { get; }
    public int EntityId { get; }

    public MediaCreatedEvent(int mediaId, string entityType, int entityId)
    {
        MediaId = mediaId;
        EntityType = entityType;
        EntityId = entityId;
    }
}