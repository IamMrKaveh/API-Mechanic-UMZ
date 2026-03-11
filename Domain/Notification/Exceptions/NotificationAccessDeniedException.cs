namespace Domain.Notification.Exceptions;

public sealed class NotificationAccessDeniedException(int notificationId, int userId) : DomainException($"کاربر {userId} دسترسی به اعلان {notificationId} را ندارد.")
{
    public int NotificationId { get; } = notificationId;
    public int UserId { get; } = userId;
}