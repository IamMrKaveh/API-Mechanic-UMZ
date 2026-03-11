namespace Domain.Notification.Events;

public sealed class NotificationsBulkReadEvent(int userId, int count) : DomainEvent
{
    public int UserId { get; } = userId;
    public int Count { get; } = count;
}