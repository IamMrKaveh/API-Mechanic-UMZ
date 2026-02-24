namespace Application.Notification.Features.Queries.GetUserNotifications;

public sealed class GetUserNotificationsHandler
    : IRequestHandler<GetUserNotificationsQuery, ServiceResult<PaginatedResult<NotificationDto>>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IMapper _mapper;

    public GetUserNotificationsHandler(
        INotificationRepository notificationRepository,
        IMapper mapper)
    {
        _notificationRepository = notificationRepository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<PaginatedResult<NotificationDto>>> Handle(
        GetUserNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _notificationRepository.GetByUserIdAsync(
            request.UserId,
            request.IsRead,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = _mapper.Map<IEnumerable<NotificationDto>>(items);

        var result = PaginatedResult<NotificationDto>.Create(
            [.. dtos],
            totalCount,
            request.Page,
            request.PageSize);

        return ServiceResult<PaginatedResult<NotificationDto>>.Success(result);
    }
}