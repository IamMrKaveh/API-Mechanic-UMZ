using Domain.User.ValueObjects;

namespace Application.Notification.Features.Commands.MarkAllNotificationsRead;

public class MarkAllNotificationsReadHandler(INotificationService notificationService) : IRequestHandler<MarkAllNotificationsReadCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(MarkAllNotificationsReadCommand request, CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);
        await notificationService.MarkAllAsReadAsync(userId, ct);
        return ServiceResult.Success();
    }
}