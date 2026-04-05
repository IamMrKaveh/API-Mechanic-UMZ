using Domain.Notification.Events;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Notification.Aggregates;

public sealed class Notification : AggregateRoot<NotificationId>, IAuditable
{
    public UserId UserId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Notification()
    { }

    public static Notification Create(NotificationId id, UserId userId, NotificationType notificationType)
    {
        var notification = new Notification
        {
            Id = id,
            UserId = userId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        notification.RaiseDomainEvent(new NotificationCreatedEvent(id, userId, notificationType));

        return notification;
    }

    public void EnsureUserAccess(UserId userId)
    {
        if (UserId.Value != userId.Value)
            throw new DomainException("شما دسترسی به این اعلان را ندارید.");
    }

    public void MarkAsRead()
    {
        if (IsRead) return;

        IsRead = true;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new NotificationReadEvent(Id));
    }
}