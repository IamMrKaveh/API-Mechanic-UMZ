using Application.Common.Validation;

namespace Application.Notification.Features.Commands.MarkNotificationRead;

public class MarkNotificationReadValidator : AbstractValidator<MarkNotificationReadCommand>
{

    public MarkNotificationReadValidator()
    {
        this.RuleForRequiredId(x => x.NotificationId);
    }
}
