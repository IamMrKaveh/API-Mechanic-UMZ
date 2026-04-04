namespace Domain.Notification.ValueObjects;

public sealed record NotificationId(Guid Value)
{
    public static NotificationId NewId() => new(Guid.NewGuid());
    public static NotificationId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}