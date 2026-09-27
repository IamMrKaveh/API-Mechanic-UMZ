using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Notification.Events;

public sealed record NotificationCreatedEvent(NotificationId NotificationId, UserId UserId, NotificationType NotificationType) : DomainEvent;
