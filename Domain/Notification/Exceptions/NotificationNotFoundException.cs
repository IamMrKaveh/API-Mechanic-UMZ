namespace Domain.Notification.Exceptions;

public sealed class NotificationNotFoundException(int notificationId) : DomainException($"اعلان با شناسه {notificationId} یافت نشد.")
{
    public int NotificationId { get; } = notificationId;
}