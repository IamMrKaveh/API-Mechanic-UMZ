namespace Application.Notification.Features.Commands.DeleteNotification;

public class DeleteNotificationHandler : IRequestHandler<DeleteNotificationCommand, ServiceResult>
{
    private readonly INotificationRepository _repo;
    private readonly IUnitOfWork _uow;

    public DeleteNotificationHandler(INotificationRepository repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ServiceResult> Handle(DeleteNotificationCommand request, CancellationToken ct)
    {
        var notif = await _repo.GetByIdAsync(request.NotificationId, ct);
        if (notif == null || notif.UserId != request.UserId) return ServiceResult.Failure("Not found or unauthorized");
        
        return ServiceResult.Success();
    }
}