using Domain.Notification.Events;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Exceptions;

namespace Domain.Notification.Aggregates;

public sealed class Notification : AggregateRoot<NotificationId>, IAuditable
{
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; } = null!;
    public string? ActionUrl { get; private set; }
    public string? RelatedEntityType { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public User.Aggregates.User User { get; private set; } = null!;
    public UserId UserId { get; private set; } = null!;

    private Notification()
    { }

    public static Notification Create(
        NotificationId id,
        UserId userId,
        NotificationType notificationType,
        string title,
        string message,
        string? actionUrl = null,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("عنوان اعلان الزامی است.");

        if (string.IsNullOrWhiteSpace(message))
            throw new DomainException("متن اعلان الزامی است.");

        var notification = new Notification
        {
            Id = id,
            UserId = userId,
            Type = notificationType,
            Title = title.Trim(),
            Message = message.Trim(),
            ActionUrl = actionUrl,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
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