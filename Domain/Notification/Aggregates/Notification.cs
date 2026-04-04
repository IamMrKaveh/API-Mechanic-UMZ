using Domain.Notification.Events;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Notification.Aggregates;

public sealed class Notification : AggregateRoot<NotificationId>, IAuditable
{
    public UserId UserId { get; private set; }
    public bool IsRead { get; private set; }

    public DateTime CreatedAt => throw new NotImplementedException();

    public DateTime? UpdatedAt => throw new NotImplementedException();

    private Notification()
    { }

    public void EnsureUserAccess(UserId userId)
    {
        if (UserId.Value != userId.Value)
            throw new DomainException("شما دسترسی به این اعلان را ندارید.");
    }

    public void MarkAsRead()
    {
        if (IsRead) return;

        IsRead = true;
        RaiseDomainEvent(new NotificationReadEvent(Id));
    }
}