namespace Application.Notification.Features.Queries.GetUnreadNotificationCount;

public sealed class GetUnreadNotificationCountHandler
    : IRequestHandler<GetUnreadNotificationCountQuery, ServiceResult<int>>
{
    private readonly INotificationRepository _notificationRepository;

    public GetUnreadNotificationCountHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<ServiceResult<int>> Handle(
        GetUnreadNotificationCountQuery request,
        CancellationToken cancellationToken)
    {
        var count = await _notificationRepository.CountUnreadByUserIdAsync(request.UserId, cancellationToken);
        return ServiceResult<int>.Success(count);
    }
}