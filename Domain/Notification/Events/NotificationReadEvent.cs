using Domain.Notification.ValueObjects;

namespace Domain.Notification.Events;

public sealed record NotificationReadEvent(NotificationId NotificationId) : DomainEvent;
