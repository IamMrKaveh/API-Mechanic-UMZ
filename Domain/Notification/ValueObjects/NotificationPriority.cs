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

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(NotificationPriority priority) => priority.Value;
}