namespace Domain.Notification.Events;

public sealed class NotificationCreatedEvent(int notificationId, int userId, string notificationType) : DomainEvent
{
    public int NotificationId { get; } = notificationId;
    public int UserId { get; } = userId;
    public string NotificationType { get; } = notificationType;
}