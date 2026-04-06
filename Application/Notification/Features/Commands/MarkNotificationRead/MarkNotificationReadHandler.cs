using Application.Common.Results;
using Application.Notification.Contracts;

namespace Application.Notification.Features.Commands.MarkNotificationRead;

public class MarkNotificationReadHandler(
    INotificationService notificationService) : IRequestHandler<MarkNotificationReadCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        await notificationService.MarkAsReadAsync(request.NotificationId, request.UserId, ct);
        return ServiceResult.Success();
    }
}