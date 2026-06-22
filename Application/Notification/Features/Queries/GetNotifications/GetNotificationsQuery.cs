using Application.Notification.Features.Shared;

namespace Application.Notification.Features.Queries.GetNotifications;

public record GetNotificationsQuery(
    bool UnreadOnly = false,
    int Page = 1,
    int PageSize = 10)
    : IPageQuery<NotificationDto>;