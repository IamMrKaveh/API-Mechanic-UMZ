using Application.Common.Results;
using Domain.Notification.Interfaces;

namespace Application.Notification.Features.Commands.MarkNotificationAsRead;

public sealed class MarkNotificationAsReadHandler(
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork)
        : IRequestHandler<MarkNotificationAsReadCommand, ServiceResult<bool>>
{
    private readonly INotificationRepository _notificationRepository = notificationRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult<bool>> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken) ?? throw new NotificationNotFoundException(request.NotificationId);
        if (notification.UserId != request.UserId)
            throw new NotificationAccessDeniedException(request.NotificationId, request.UserId);

        notification.MarkAsRead();

        _notificationRepository.Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }
}