using Application.Common.Validation;

namespace Application.Notification.Features.Commands.AdminSendNotification;

public class AdminSendNotificationValidator : AbstractValidator<AdminSendNotificationCommand>
{

    public AdminSendNotificationValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Type).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ActionUrl).MaximumLength(500);
        RuleFor(x => x).Must(x => x.SendToAll || x.UserId.HasValue)
            .WithMessage("شناسه کاربر الزامی است.");
    }
}
