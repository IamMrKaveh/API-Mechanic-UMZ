using Domain.Common.Exceptions;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Notification.Exceptions;

public sealed class NotificationAccessDeniedException : DomainException
{
    public NotificationId NotificationId { get; }
    public UserId UserId { get; }

    public override string ErrorCode => "NOTIFICATION_ACCESS_DENIED";

    public NotificationAccessDeniedException(NotificationId notificationId, UserId userId)
        : base($"کاربر {userId} دسترسی به اعلان {notificationId} را ندارد.")
    {
        NotificationId = notificationId;
        UserId = userId;
    }
}