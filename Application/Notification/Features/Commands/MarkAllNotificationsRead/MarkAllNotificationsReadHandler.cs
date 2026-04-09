namespace Application.Notification.Features.Commands.MarkAllNotificationsRead;

public class MarkAllNotificationsReadHandler(INotificationService notificationService) : IRequestHandler<MarkAllNotificationsReadCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(MarkAllNotificationsReadCommand request, CancellationToken ct)
    {
        await notificationService.MarkAllAsReadAsync(request.UserId, ct);
        return ServiceResult.Success();
    }
}