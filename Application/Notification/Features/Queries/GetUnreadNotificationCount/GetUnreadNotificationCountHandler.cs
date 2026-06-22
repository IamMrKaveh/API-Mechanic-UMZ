using Domain.User.ValueObjects;

namespace Application.Notification.Features.Queries.GetUnreadNotificationCount;

public class GetUnreadNotificationCountHandler(
    INotificationQueryService notificationQueryService,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetUnreadNotificationCountQuery, int>
{
    public async Task<ServiceResult<int>> Handle(
        GetUnreadNotificationCountQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(currentUserService.UserId.Value);
        var count = await notificationQueryService.GetUnreadCountAsync(userId, ct);
        return ServiceResult<int>.Success(count);
    }
}