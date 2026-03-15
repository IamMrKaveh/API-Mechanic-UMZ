using Domain.Notification.Events;

namespace Domain.Notification;

public class Notification : AggregateRoot, IAuditable, ISoftDeletable
{
    public int UserId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public ValueObjects.NotificationType Type { get; private set; } = null!;
    public bool IsRead { get; private set; }
    public string? ActionUrl { get; private set; }
    public int? RelatedEntityId { get; private set; }
    public string? RelatedEntityType { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    private const int MaxTitleLength = 200;
    private const int MaxMessageLength = 1000;
    private const int MaxActionUrlLength = 500;
    private const int MaxEntityTypeLength = 100;

    private Notification()
    { }

    public static Notification Create(
        int userId,
        string title,
        string message,
        ValueObjects.NotificationType type,
        string? actionUrl = null,
        int? relatedEntityId = null,
        string? relatedEntityType = null)
    {
        Guard.Against.NegativeOrZero(userId, nameof(userId));
        Guard.Against.Null(type, nameof(type));
        ValidateTitle(title);
        ValidateMessage(message);
        ValidateActionUrl(actionUrl);
        ValidateRelatedEntityType(relatedEntityType);

        var notification = new Notification
        {
            UserId = userId,
            Title = title.Trim(),
            Message = message.Trim(),
            Type = type,
            ActionUrl = actionUrl?.Trim(),
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType?.Trim(),
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        notification.AddDomainEvent(new NotificationCreatedEvent(
            notification.Id,
            notification.UserId,
            notification.Type.Value));

        return notification;
    }

    public void MarkAsRead()
    {
        if (IsRead) return;

        IsRead = true;
        ReadAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new NotificationReadEvent(Id, UserId));
    }

    public void Delete(int? deletedBy = null)
    {
        if (IsDeleted) return;

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new NotificationDeletedEvent(Id, UserId));
    }

    public TimeSpan? GetTimeSinceCreation() => DateTime.UtcNow - CreatedAt;

    public bool IsOlderThan(TimeSpan threshold) => GetTimeSinceCreation() > threshold;

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("عنوان اعلان الزامی است.");

        if (title.Length > MaxTitleLength)
            throw new DomainException($"عنوان اعلان نمی‌تواند بیش از {MaxTitleLength} کاراکتر باشد.");
    }

    private static void ValidateMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new DomainException("متن اعلان الزامی است.");

        if (message.Length > MaxMessageLength)
            throw new DomainException($"متن اعلان نمی‌تواند بیش از {MaxMessageLength} کاراکتر باشد.");
    }

    private static void ValidateActionUrl(string? actionUrl)
    {
        if (!string.IsNullOrWhiteSpace(actionUrl) && actionUrl.Length > MaxActionUrlLength)
            throw new DomainException($"آدرس اقدام نمی‌تواند بیش از {MaxActionUrlLength} کاراکتر باشد.");
    }

    private static void ValidateRelatedEntityType(string? entityType)
    {
        if (!string.IsNullOrWhiteSpace(entityType) && entityType.Length > MaxEntityTypeLength)
            throw new DomainException($"نوع موجودیت مرتبط نمی‌تواند بیش از {MaxEntityTypeLength} کاراکتر باشد.");
    }
}