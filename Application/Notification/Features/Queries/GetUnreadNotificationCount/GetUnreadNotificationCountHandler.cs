using Domain.User.ValueObjects;

namespace Application.Notification.Features.Queries.GetUnreadNotificationCount;

public class GetUnreadNotificationCountHandler(INotificationQueryService notificationQueryService)
    : IRequestHandler<GetUnreadNotificationCountQuery, ServiceResult<int>>
{
    public async Task<ServiceResult<int>> Handle(
        GetUnreadNotificationCountQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);
        var count = await notificationQueryService.GetUnreadCountAsync(userId, ct);
        return ServiceResult<int>.Success(count);
    }
}