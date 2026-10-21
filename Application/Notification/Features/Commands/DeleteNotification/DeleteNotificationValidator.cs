using Application.Common.Validation;

namespace Application.Notification.Features.Commands.DeleteNotification;

public class DeleteNotificationValidator : AbstractValidator<DeleteNotificationCommand>
{

    public DeleteNotificationValidator()
    {
        this.RuleForRequiredId(x => x.NotificationId);
    }
}
