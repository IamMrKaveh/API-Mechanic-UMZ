using Application.Notification.Features.Shared;
using Domain.Notification.Interfaces;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Notification.Features.Queries.GetNotificationPreferences;

public sealed class GetNotificationPreferencesHandler(
    INotificationPreferenceRepository preferenceRepository,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetNotificationPreferencesQuery, NotificationPreferenceDto>
{
    public async Task<ServiceResult<NotificationPreferenceDto>> Handle(
        GetNotificationPreferencesQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(currentUserService.UserId!.Value);

        var preference = await preferenceRepository.GetByUserIdAsync(userId, ct);

        var options = NotificationType.GetAll()
            .Select(type => new NotificationTypeOptionDto
            {
                Value = type.Value,
                DisplayName = type.DisplayName,
                Category = type.Category.ToString(),
                IsEnabled = preference is null || preference.IsEnabled(type)
            })
            .OrderBy(o => o.Category, StringComparer.Ordinal)
            .ThenBy(o => o.DisplayName, StringComparer.Ordinal)
            .ToList();

        return ServiceResult<NotificationPreferenceDto>.Success(
            new NotificationPreferenceDto { Types = options });
    }
}
