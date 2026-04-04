using Application.Common.Results;
using Domain.Notification.Interfaces;

namespace Application.Notification.Features.Commands.DeleteNotification;

public class DeleteNotificationHandler(INotificationRepository repo) : IRequestHandler<DeleteNotificationCommand, ServiceResult>
{
    private readonly INotificationRepository _repo = repo;

    public async Task<ServiceResult> Handle(
        DeleteNotificationCommand request,
        CancellationToken ct)
    {
        var notif = await _repo.GetByIdAsync(request.NotificationId, ct);
        if (notif == null)
            return ServiceResult.NotFound("Not found or unauthorized");
        if (notif.UserId != request.UserId)
            return ServiceResult.NotFound("unauthorized");
        return ServiceResult.Success();
    }
}