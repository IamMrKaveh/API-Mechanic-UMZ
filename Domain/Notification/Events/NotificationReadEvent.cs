namespace Domain.Notification.Events;

public sealed class NotificationReadEvent(int notificationId, int userId) : DomainEvent
{
    public int NotificationId { get; } = notificationId;
    public int UserId { get; } = userId;
}