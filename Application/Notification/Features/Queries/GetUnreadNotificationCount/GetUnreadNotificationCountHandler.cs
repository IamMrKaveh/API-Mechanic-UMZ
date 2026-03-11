using Application.Common.Models;

namespace Application.Notification.Features.Queries.GetUnreadNotificationCount;

public sealed class GetUnreadNotificationCountHandler(INotificationQueryService notificationQueryService)
        : IRequestHandler<GetUnreadNotificationCountQuery, ServiceResult<int>>
{
    private readonly INotificationQueryService _notificationQueryService = notificationQueryService;

    public async Task<ServiceResult<int>> Handle(
        GetUnreadNotificationCountQuery request,
        CancellationToken cancellationToken)
    {
        var count = await _notificationQueryService.CountUnreadByUserIdAsync(
            request.UserId, cancellationToken);

        return ServiceResult<int>.Success(count);
    }
}