using Domain.Notification.ValueObjects;

namespace Domain.Notification.Exceptions;

public sealed class NotificationNotFoundException : DomainException
{
    public NotificationId NotificationId { get; }

    public override string ErrorCode => "NOTIFICATION_NOT_FOUND";

    public NotificationNotFoundException(NotificationId notificationId)
        : base($"اعلان با شناسه {notificationId} یافت نشد.")
    {
        NotificationId = notificationId;
    }
}