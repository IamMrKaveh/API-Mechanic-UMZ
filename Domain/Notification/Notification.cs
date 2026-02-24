namespace Domain.Notification;

public class Notification : AggregateRoot, IAuditable
{
    public int UserId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public string Type { get; private set; } = null!;
    public bool IsRead { get; private set; }
    public string? ActionUrl { get; private set; }
    public int? RelatedEntityId { get; private set; }
    public string? RelatedEntityType { get; private set; }
    public DateTime? ReadAt { get; private set; }

    
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    
    private const int MaxTitleLength = 200;

    private const int MaxMessageLength = 1000;
    private const int MaxActionUrlLength = 500;
    private const int MaxEntityTypeLength = 100;

    
    public User.User User { get; private set; } = null!;

    private Notification()
    { }

    #region Factory Methods

    public static Notification Create(
        int userId,
        string title,
        string message,
        string type,
        string? actionUrl = null,
        int? relatedEntityId = null,
        string? relatedEntityType = null)
    {
        Guard.Against.NegativeOrZero(userId, nameof(userId));
        ValidateTitle(title);
        ValidateMessage(message);
        ValidateType(type);
        ValidateActionUrl(actionUrl);
        ValidateRelatedEntityType(relatedEntityType);

        var notification = new Notification
        {
            UserId = userId,
            Title = title.Trim(),
            Message = message.Trim(),
            Type = type.Trim(),
            ActionUrl = actionUrl?.Trim(),
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType?.Trim(),
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        notification.AddDomainEvent(new Events.NotificationCreatedEvent(
            notification.Id,
            notification.UserId,
            notification.Type));

        return notification;
    }

    public static Notification CreateOrderNotification(
        int userId,
        string title,
        string message,
        string type,
        int orderId,
        string? actionUrl = null)
    {
        return Create(userId, title, message, type, actionUrl, orderId, "Order");
    }

    public static Notification CreateTicketNotification(
        int userId,
        string title,
        string message,
        int ticketId,
        string? actionUrl = null)
    {
        return Create(userId, title, message, "TicketReply", actionUrl, ticketId, "Ticket");
    }

    public static Notification CreateProductNotification(
        int userId,
        string title,
        string message,
        string type,
        int productId,
        string? actionUrl = null)
    {
        return Create(userId, title, message, type, actionUrl, productId, "Product");
    }

    public static Notification CreateSystemAlert(
        int userId,
        string title,
        string message,
        string? actionUrl = null)
    {
        return Create(userId, title, message, "SystemAlert", actionUrl);
    }

    #endregion Factory Methods

    #region Domain Behaviors

    public void MarkAsRead()
    {
        if (IsRead) return;

        IsRead = true;
        ReadAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new Events.NotificationReadEvent(Id, UserId));
    }

    public void MarkAsUnread()
    {
        if (!IsRead) return;

        IsRead = false;
        ReadAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateContent(string title, string message)
    {
        ValidateTitle(title);
        ValidateMessage(message);

        Title = title.Trim();
        Message = message.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateActionUrl(string? actionUrl)
    {
        ValidateActionUrl(actionUrl);

        ActionUrl = actionUrl?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Domain Behaviors

    #region Query Methods

    public bool IsOrderRelated() => RelatedEntityType == "Order";

    public bool IsTicketRelated() => RelatedEntityType == "Ticket";

    public bool IsProductRelated() => RelatedEntityType == "Product";

    public bool HasRelatedEntity() => RelatedEntityId.HasValue && !string.IsNullOrEmpty(RelatedEntityType);

    public bool HasActionUrl() => !string.IsNullOrWhiteSpace(ActionUrl);

    public TimeSpan? GetTimeSinceCreation() => DateTime.UtcNow - CreatedAt;

    public TimeSpan? GetTimeSinceRead() => ReadAt.HasValue ? DateTime.UtcNow - ReadAt.Value : null;

    public bool IsRecent(TimeSpan threshold) => GetTimeSinceCreation() <= threshold;

    public bool IsOlderThan(TimeSpan threshold) => GetTimeSinceCreation() > threshold;

    #endregion Query Methods

    #region Validation Methods

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

    private static void ValidateType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new DomainException("نوع اعلان الزامی است.");

        
        try
        {
            ValueObjects.NotificationType.FromString(type);
        }
        catch
        {
            
        }
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

    #endregion Validation Methods
}