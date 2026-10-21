using Application.Common.Validation;

namespace Application.Notification.Features.Commands.AdminDeleteNotification;

public class AdminDeleteNotificationValidator : AbstractValidator<AdminDeleteNotificationCommand>
{

    public AdminDeleteNotificationValidator()
    {
        this.RuleForRequiredId(x => x.NotificationId);
    }
}
