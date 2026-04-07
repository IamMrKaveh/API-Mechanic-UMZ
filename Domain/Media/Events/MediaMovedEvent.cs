using Domain.Media.ValueObjects;

namespace Domain.Media.Events;

public sealed class MediaMovedEvent(MediaId mediaId, string oldEntityType, int oldEntityId, string newEntityType, int newEntityId) : DomainEvent
{
    public MediaId MediaId { get; } = mediaId;
    public string OldEntityType { get; } = oldEntityType;
    public int OldEntityId { get; } = oldEntityId;
    public string NewEntityType { get; } = newEntityType;
    public int NewEntityId { get; } = newEntityId;
}