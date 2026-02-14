namespace Domain.Media.Events;

public sealed class MediaDeletedEvent : DomainEvent
{
    public int MediaId { get; }
    public string EntityType { get; }
    public int EntityId { get; }
    public int? DeletedBy { get; }

    public MediaDeletedEvent(int mediaId, string entityType, int entityId, int? deletedBy)
    {
        MediaId = mediaId;
        EntityType = entityType;
        EntityId = entityId;
        DeletedBy = deletedBy;
    }
}