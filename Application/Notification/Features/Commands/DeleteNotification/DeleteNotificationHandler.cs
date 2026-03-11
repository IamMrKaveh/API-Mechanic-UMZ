using Application.Common.Models;
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
        if (notif == null || notif.UserId != request.UserId)
            return ServiceResult.Failure("Not found or unauthorized");

        return ServiceResult.Success();
    }
}