namespace Domain.Notification.ValueObjects;

public sealed record NotificationId : StronglyTypedId<NotificationId>
{
    private NotificationId(Guid value) : base(value) { }

    public static implicit operator Guid(NotificationId id) => id.Value;

    public override string ToString() => Value.ToString();
}
