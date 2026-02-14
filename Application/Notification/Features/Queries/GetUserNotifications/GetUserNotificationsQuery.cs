namespace Application.Notification.Features.Queries.GetUserNotifications;

public sealed record GetUserNotificationsQuery(
    int UserId,
    bool? IsRead,
    int Page = 1,
    int PageSize = 20) : IRequest<ServiceResult<PaginatedResult<NotificationDto>>>;