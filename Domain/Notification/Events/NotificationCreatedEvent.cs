using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Notification.Events;

public sealed class NotificationCreatedEvent(NotificationId notificationId, UserId userId, NotificationType notificationType) : DomainEvent
{
    public NotificationId NotificationId { get; } = notificationId;
    public UserId UserId { get; } = userId;
    public NotificationType NotificationType { get; } = notificationType;
}