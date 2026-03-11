using Application.Common.Models;

namespace Application.Notification.Features.Queries.GetUserNotifications;

public sealed class GetUserNotificationsHandler(
    INotificationQueryService notificationQueryService)
        : IRequestHandler<GetUserNotificationsQuery, ServiceResult<PaginatedResult<NotificationDto>>>
{
    private readonly INotificationQueryService _notificationQueryService = notificationQueryService;

    public async Task<ServiceResult<PaginatedResult<NotificationDto>>> Handle(
        GetUserNotificationsQuery request,
        CancellationToken ct)
    {
        var result = await _notificationQueryService.GetByUserIdAsync(
            request.UserId,
            request.IsRead,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<NotificationDto>>.Success(result);
    }
}