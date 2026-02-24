namespace Domain.Notification.Exceptions;

public sealed class NotificationAccessDeniedException : DomainException
{
    public int NotificationId { get; }
    public int UserId { get; }

    public NotificationAccessDeniedException(int notificationId, int userId)
        : base($"کاربر {userId} دسترسی به اعلان {notificationId} را ندارد.")
    {
        NotificationId = notificationId;
        UserId = userId;
    }
}