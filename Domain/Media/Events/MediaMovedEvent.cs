namespace Domain.Media.Events;

public sealed class MediaMovedEvent(Guid mediaId, string oldEntityType, int oldEntityId, string newEntityType, int newEntityId) : DomainEvent
{
    public Guid MediaId { get; } = mediaId;
    public string OldEntityType { get; } = oldEntityType;
    public int OldEntityId { get; } = oldEntityId;
    public string NewEntityType { get; } = newEntityType;
    public int NewEntityId { get; } = newEntityId;
}