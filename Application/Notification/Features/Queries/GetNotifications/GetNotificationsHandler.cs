using Application.Notification.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Notification.Features.Queries.GetNotifications;

public class GetNotificationsHandler(INotificationQueryService notificationQueryService)
    : IRequestHandler<GetNotificationsQuery, ServiceResult<PaginatedResult<NotificationDto>>>
{
    public async Task<ServiceResult<PaginatedResult<NotificationDto>>> Handle(
        GetNotificationsQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);
        var result = await notificationQueryService.GetByUserIdAsync(
            userId,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<NotificationDto>>.Success(result);
    }
}