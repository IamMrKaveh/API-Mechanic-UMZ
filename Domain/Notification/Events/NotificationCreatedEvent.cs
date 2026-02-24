namespace Domain.Notification.Events;

public sealed class NotificationCreatedEvent : DomainEvent
{
    public int NotificationId { get; }
    public int UserId { get; }
    public string NotificationType { get; }

    public NotificationCreatedEvent(int notificationId, int userId, string notificationType)
    {
        NotificationId = notificationId;
        UserId = userId;
        NotificationType = notificationType;
    }
}