namespace Domain.Notification.Exceptions;

public sealed class NotificationNotFoundException : DomainException
{
    public int NotificationId { get; }

    public NotificationNotFoundException(int notificationId)
        : base($"اعلان با شناسه {notificationId} یافت نشد.")
    {
        NotificationId = notificationId;
    }
}