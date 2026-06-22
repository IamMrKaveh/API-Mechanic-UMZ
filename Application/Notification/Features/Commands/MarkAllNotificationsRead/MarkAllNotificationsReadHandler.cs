using Domain.User.ValueObjects;

namespace Application.Notification.Features.Commands.MarkAllNotificationsRead;

public class MarkAllNotificationsReadHandler(
    INotificationService notificationService,
    ICurrentUserService currentUserService)
    : ICommandHandler<MarkAllNotificationsReadCommand>
{
    public async Task<ServiceResult> Handle(MarkAllNotificationsReadCommand request, CancellationToken ct)
    {
        var userId = UserId.From(currentUserService.UserId.Value);
        await notificationService.MarkAllAsReadAsync(userId, ct);
        return ServiceResult.Success();
    }
}