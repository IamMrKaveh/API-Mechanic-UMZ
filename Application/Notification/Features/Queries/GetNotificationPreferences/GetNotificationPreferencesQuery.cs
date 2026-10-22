using Application.Notification.Features.Shared;

namespace Application.Notification.Features.Queries.GetNotificationPreferences;

public sealed record GetNotificationPreferencesQuery : IQuery<NotificationPreferenceDto>;
