namespace Domain.Notification.Events;

public sealed class NotificationReadEvent : DomainEvent
{
    public int NotificationId { get; }
    public int UserId { get; }

    public NotificationReadEvent(int notificationId, int userId)
    {
        NotificationId = notificationId;
        UserId = userId;
    }
}