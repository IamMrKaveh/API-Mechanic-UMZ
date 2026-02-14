namespace Application.Notification.Features.Commands.MarkAllNotificationsAsRead;

public sealed class MarkAllNotificationsAsReadHandler
    : IRequestHandler<MarkAllNotificationsAsReadCommand, ServiceResult<int>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkAllNotificationsAsReadHandler(
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<int>> Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        var count = await _notificationRepository.MarkAllAsReadByUserIdAsync(request.UserId, cancellationToken);
        return ServiceResult<int>.Success(count);
    }
}