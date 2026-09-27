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
        var notificationResult = await (notificationRepository.GetByIdAsync(notificationId, ct)).OrNotFoundAsync("اعلان یافت نشد.");
        if (notificationResult.IsFailure) return notificationResult.Error;
        var notification = notificationResult.Value;

        notificationRepository.Remove(notification);

        return ServiceResult.Success();
    }
}