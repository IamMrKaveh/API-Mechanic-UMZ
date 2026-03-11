namespace Domain.Notification.Events;

public class NotificationDeletedEvent(int notificationId, int userId) : DomainEvent
{
    public int NotificationId { get; } = notificationId;
    public int UserId { get; } = userId;
}