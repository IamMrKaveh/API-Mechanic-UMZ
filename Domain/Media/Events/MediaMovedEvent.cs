namespace Domain.Media.Events;

public sealed class MediaMovedEvent : DomainEvent
{
    public int MediaId { get; }
    public string OldEntityType { get; }
    public int OldEntityId { get; }
    public string NewEntityType { get; }
    public int NewEntityId { get; }

    public MediaMovedEvent(int mediaId, string oldEntityType, int oldEntityId, string newEntityType, int newEntityId)
    {
        MediaId = mediaId;
        OldEntityType = oldEntityType;
        OldEntityId = oldEntityId;
        NewEntityType = newEntityType;
        NewEntityId = newEntityId;
    }
}