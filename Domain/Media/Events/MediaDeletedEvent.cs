namespace Domain.Media.Events;

public sealed class MediaDeletedEvent(Guid mediaId, string entityType, int entityId, int? deletedBy) : DomainEvent
{
    public Guid MediaId { get; } = mediaId;
    public string EntityType { get; } = entityType;
    public int EntityId { get; } = entityId;
    public int? DeletedBy { get; } = deletedBy;
}