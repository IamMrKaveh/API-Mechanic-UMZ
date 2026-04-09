namespace Application.Notification.Features.Commands.DeleteNotification;

public class DeleteNotificationHandler(
    INotificationService notificationService) : IRequestHandler<DeleteNotificationCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(DeleteNotificationCommand request, CancellationToken ct)
    {
        await notificationService.DeleteAsync(request.NotificationId, request.UserId, ct);
        return ServiceResult.Success();
    }
}