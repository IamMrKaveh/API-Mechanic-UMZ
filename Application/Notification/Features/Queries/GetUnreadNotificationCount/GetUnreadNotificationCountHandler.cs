using Application.Common.Interfaces;
using Domain.User.ValueObjects;
using SharedKernel.Results;

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
        if (!currentUserService.IsAuthenticated || currentUserService.UserId is null)
        {
            return ServiceResult<int>.Success(0);
        }

        var userId = UserId.From(currentUserService.UserId.Value);
        var count = await notificationQueryService.GetUnreadCountAsync(userId, ct);
        return ServiceResult<int>.Success(count);
    }
}