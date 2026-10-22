using Domain.Notification.Aggregates;
using Domain.Notification.Interfaces;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Notification.Features.Commands.UpdateNotificationPreferences;

public sealed class UpdateNotificationPreferencesHandler(
    INotificationPreferenceRepository preferenceRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider,
    ICurrentUserService currentUserService)
    : ICommandHandler<UpdateNotificationPreferencesCommand, Unit>
{
    public async Task<ServiceResult<Unit>> Handle(
        UpdateNotificationPreferencesCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(currentUserService.UserId!.Value);
        var now = dateTimeProvider.UtcNow;

        var types = request.DisabledTypeNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(NotificationType.FromString)
            .ToList();

        var preference = await preferenceRepository.GetByUserIdAsync(userId, ct);

        if (preference is null)
        {
            preference = NotificationPreference.Create(
                NotificationPreferenceId.NewId(), userId, now);
            preference.ReplaceDisabledTypes(types, now);
            await preferenceRepository.AddAsync(preference, ct);
        }
        else
        {
            preference.ReplaceDisabledTypes(types, now);
            preferenceRepository.Update(preference);
        }

        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogSystemEventAsync(
            "NotificationPreferencesUpdated",
            $"ترجیحات اعلان کاربر {userId.Value} به‌روزرسانی شد.",
            ct);

        return ServiceResult<Unit>.Success(Unit.Value);
    }
}
