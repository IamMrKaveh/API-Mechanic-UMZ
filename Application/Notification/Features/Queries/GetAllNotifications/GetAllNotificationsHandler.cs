using Application.Notification.Features.Shared;

namespace Application.Notification.Features.Queries.GetAllNotifications;

public class GetAllNotificationsHandler(INotificationQueryService notificationQueryService)
    : IRequestHandler<GetAllNotificationsQuery, ServiceResult<PaginatedResult<NotificationDto>>>
{
    public async Task<ServiceResult<PaginatedResult<NotificationDto>>> Handle(
        GetAllNotificationsQuery request,
        CancellationToken ct)
    {
        var result = await notificationQueryService.GetAllAsync(request.Page, request.PageSize, ct);
        return ServiceResult<PaginatedResult<NotificationDto>>.Success(result);
    }
}