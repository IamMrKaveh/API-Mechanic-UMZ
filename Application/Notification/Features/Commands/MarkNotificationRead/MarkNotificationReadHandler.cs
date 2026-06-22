using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Notification.Features.Commands.MarkNotificationRead;

public class MarkNotificationReadHandler(
    INotificationService notificationService,
    ICurrentUserService currentUserService)
    : ICommandHandler<MarkNotificationReadCommand>
{
    public async Task<ServiceResult> Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        var notificationId = NotificationId.From(request.NotificationId);
        var userId = UserId.From(currentUserService.UserId.Value);
        await notificationService.MarkAsReadAsync(notificationId, userId, ct);
        return ServiceResult.Success();
    }
}