using Application.Notification.Features.Shared;

namespace Application.Notification.Features.Queries.GetNotifications;

public record GetNotificationsQuery(
    Guid UserId,
    bool UnreadOnly = false,
    int Page = 1,
    int PageSize = 10) : IRequest<ServiceResult<PaginatedResult<NotificationDto>>>;