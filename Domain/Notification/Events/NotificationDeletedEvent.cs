using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Notification.Events;

public class NotificationDeletedEvent(NotificationId notificationId, UserId userId) : DomainEvent
{
    public NotificationId NotificationId { get; } = notificationId;
    public UserId UserId { get; } = userId;
}