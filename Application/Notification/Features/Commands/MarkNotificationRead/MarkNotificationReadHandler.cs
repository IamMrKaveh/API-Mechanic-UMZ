using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Notification.Features.Commands.MarkNotificationRead;

public class MarkNotificationReadHandler(
    INotificationService notificationService) : IRequestHandler<MarkNotificationReadCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        var notificationId = NotificationId.From(request.NotificationId);
        var userId = UserId.From(request.UserId);
        await notificationService.MarkAsReadAsync(notificationId, userId, ct);
        return ServiceResult.Success();
    }
}