namespace Application.Notification.Features.Queries.GetUnreadNotificationCount;

public sealed class GetUnreadNotificationCountHandler(INotificationQueryService notificationQueryService)
        : IRequestHandler<GetUnreadNotificationCountQuery, ServiceResult<int>>
{
    public async Task<ServiceResult<int>> Handle(
        GetUnreadNotificationCountQuery request,
        CancellationToken cancellationToken)
    {
        var count = await notificationQueryService.CountUnreadByUserIdAsync(
            request.UserId, cancellationToken);

        return ServiceResult<int>.Success(count);
    }
}