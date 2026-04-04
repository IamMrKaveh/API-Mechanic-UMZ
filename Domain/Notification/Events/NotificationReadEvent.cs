using Domain.Notification.ValueObjects;

namespace Domain.Notification.Events;

public sealed class NotificationReadEvent(NotificationId notificationId) : DomainEvent
{
    public NotificationId NotificationId { get; } = notificationId;
}