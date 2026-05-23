using Application.Notification.Features.Shared;

namespace Application.Notification.Features.Queries.GetAllNotifications;

public record GetAllNotificationsQuery(
    int Page = 1,
    int PageSize = 20) : IRequest<ServiceResult<PaginatedResult<NotificationDto>>>;