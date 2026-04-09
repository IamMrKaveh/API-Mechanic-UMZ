namespace Domain.Notification.ValueObjects;

public sealed record NotificationId
{
    public Guid Value { get; }

    private NotificationId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("NotificationId cannot be empty.", nameof(value));

        Value = value;
    }

    public static NotificationId NewId() => new(Guid.NewGuid());

    public static NotificationId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}