using Application.Notification.Features.Shared;

namespace Application.Notification.Features.Queries.GetNotifications;

public class GetNotificationsHandler(
    INotificationQueryService notificationQueryService) : IRequestHandler<GetNotificationsQuery, ServiceResult<PaginatedResult<NotificationDto>>>
{
    private readonly INotificationQueryService _notificationQueryService = notificationQueryService;

    public async Task<ServiceResult<PaginatedResult<NotificationDto>>> Handle(
        GetNotificationsQuery request,
        CancellationToken ct)
    {
        var result = await _notificationQueryService.GetUserNotificationsAsync(
            request.UserId,
            request.UnreadOnly,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<NotificationDto>>.Success(result);
    }
}