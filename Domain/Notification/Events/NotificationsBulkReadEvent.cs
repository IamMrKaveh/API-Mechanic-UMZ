using Domain.User.ValueObjects;

namespace Domain.Notification.Events;

public sealed class NotificationsBulkReadEvent(UserId userId, int count) : DomainEvent
{
    public UserId UserId { get; } = userId;
    public int Count { get; } = count;
}