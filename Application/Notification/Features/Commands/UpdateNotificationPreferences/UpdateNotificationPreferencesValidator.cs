using Application.Common.Validation;
using Domain.Notification.ValueObjects;

namespace Application.Notification.Features.Commands.UpdateNotificationPreferences;

public sealed class UpdateNotificationPreferencesValidator
    : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    private static readonly HashSet<string> KnownTypeNames =
        NotificationType.GetAll().Select(t => t.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

    public UpdateNotificationPreferencesValidator()
    {
        RuleFor(x => x.DisabledTypeNames)
            .NotNull();

        RuleForEach(x => x.DisabledTypeNames)
            .Must(name => KnownTypeNames.Contains(name))
            .WithMessage("نوع اعلان ناشناخته است.");
    }
}
