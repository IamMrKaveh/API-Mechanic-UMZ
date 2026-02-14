namespace Domain.Notification.ValueObjects;

public sealed class NotificationPriority : ValueObject
{
    public string Value { get; }
    public string DisplayName { get; }
    public int SortOrder { get; }

    private NotificationPriority(string value, string displayName, int sortOrder)
    {
        Value = value;
        DisplayName = displayName;
        SortOrder = sortOrder;
    }

    public static NotificationPriority Low => new("Low", "کم", 1);
    public static NotificationPriority Normal => new("Normal", "معمولی", 2);
    public static NotificationPriority High => new("High", "زیاد", 3);
    public static NotificationPriority Urgent => new("Urgent", "فوری", 4);

    public static NotificationPriority FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Normal;

        return value.ToLowerInvariant() switch
        {
            "low" => Low,
            "normal" => Normal,
            "high" => High,
            "urgent" => Urgent,
            _ => Normal
        };
    }

    public static NotificationPriority FromNotificationType(NotificationType type)
    {
        Guard.Against.Null(type, nameof(type));

        if (type.IsHighPriority())
            return High;

        if (type.IsOrderRelated())
            return Normal;

        return Low;
    }

    public static IEnumerable<NotificationPriority> GetAll()
    {
        yield return Low;
        yield return Normal;
        yield return High;
        yield return Urgent;
    }

    public bool IsHighPriority() => SortOrder >= High.SortOrder;

    public bool IsUrgent() => this == Urgent;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => DisplayName;

    public static implicit operator string(NotificationPriority priority) => priority.Value;

    public static bool operator >(NotificationPriority left, NotificationPriority right) =>
        left.SortOrder > right.SortOrder;

    public static bool operator <(NotificationPriority left, NotificationPriority right) =>
        left.SortOrder < right.SortOrder;

    public static bool operator >=(NotificationPriority left, NotificationPriority right) =>
        left.SortOrder >= right.SortOrder;

    public static bool operator <=(NotificationPriority left, NotificationPriority right) =>
        left.SortOrder <= right.SortOrder;
}