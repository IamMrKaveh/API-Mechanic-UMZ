namespace Domain.Notification.ValueObjects;

public sealed record NotificationId : IStronglyTypedId
{
    public Guid Value { get; }

    private NotificationId(Guid value) => Value = value;

    public static NotificationId NewId() => new(Guid.NewGuid());

    public static NotificationId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("NotificationId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(NotificationId id) => id.Value;
}