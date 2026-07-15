using Domain.Notification.Interfaces;
using Domain.Notification.ValueObjects;

namespace Application.Notification.Features.Commands.AdminDeleteNotification;

public class AdminDeleteNotificationHandler(
    INotificationRepository notificationRepository)
    : ICommandHandler<AdminDeleteNotificationCommand>
{
    public async Task<ServiceResult> Handle(AdminDeleteNotificationCommand request, CancellationToken ct)
    {
        var notificationId = NotificationId.From(request.NotificationId);
        var notification = await notificationRepository.GetByIdAsync(notificationId, ct);

        if (notification is null)
            return ServiceResult.NotFound("اعلان یافت نشد.");

        notificationRepository.Remove(notification);

        return ServiceResult.Success();
    }
}