namespace Application.Notification.Features.Commands.MarkNotificationAsRead;

public sealed class MarkNotificationAsReadHandler
    : IRequestHandler<MarkNotificationAsReadCommand, ServiceResult<bool>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkNotificationAsReadHandler(
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<bool>> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification is null)
            throw new NotificationNotFoundException(request.NotificationId);

        if (notification.UserId != request.UserId)
            throw new NotificationAccessDeniedException(request.NotificationId, request.UserId);

        notification.MarkAsRead();

        _notificationRepository.Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }
}