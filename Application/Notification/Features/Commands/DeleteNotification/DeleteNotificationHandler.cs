using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Notification.Features.Commands.DeleteNotification;

public class DeleteNotificationHandler(
    INotificationService notificationService) : IRequestHandler<DeleteNotificationCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(DeleteNotificationCommand request, CancellationToken ct)
    {
        var notificationId = NotificationId.From(request.NotificationId);
        var userId = UserId.From(request.UserId);
        await notificationService.DeleteAsync(notificationId, userId, ct);
        return ServiceResult.Success();
    }
}