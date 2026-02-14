namespace Domain.Notification.Events;

public sealed class NotificationsBulkReadEvent : DomainEvent
{
    public int UserId { get; }
    public int Count { get; }

    public NotificationsBulkReadEvent(int userId, int count)
    {
        UserId = userId;
        Count = count;
    }
}