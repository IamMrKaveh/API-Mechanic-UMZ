using Domain.Media.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Media.Events;

public sealed class MediaDeletedEvent(MediaId mediaId, string entityType, int entityId, UserId? deletedBy) : DomainEvent
{
    public MediaId MediaId { get; } = mediaId;
    public string EntityType { get; } = entityType;
    public int EntityId { get; } = entityId;
    public UserId? DeletedBy { get; } = deletedBy;
}