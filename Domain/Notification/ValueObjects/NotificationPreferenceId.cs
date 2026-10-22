namespace Domain.Notification.ValueObjects;

public sealed record NotificationPreferenceId : StronglyTypedId<NotificationPreferenceId>
{
    private NotificationPreferenceId(Guid value) : base(value) { }

    public static implicit operator Guid(NotificationPreferenceId id) => id.Value;

    public override string ToString() => Value.ToString();
}
