using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.Notification.Features.Commands.AdminSendNotification;

public class AdminSendNotificationHandler(
    INotificationService notificationService,
    IUserRepository userRepository)
    : ICommandHandler<AdminSendNotificationCommand>
{
    public async Task<ServiceResult> Handle(AdminSendNotificationCommand request, CancellationToken ct)
    {
        if (request.SendToAll)
        {
            var userIds = await userRepository.GetAllActiveUserIdsAsync(ct);

            foreach (var uid in userIds)
            {
                await notificationService.CreateNotificationAsync(
                    UserId.From(uid),
                    request.Title,
                    request.Message,
                    request.Type,
                    request.ActionUrl,
                    ct: ct);
            }

            return ServiceResult.Success();
        }

        if (request.UserId is null)
            return ServiceResult.Failure("شناسه کاربر الزامی است.");

        var userId = UserId.From(request.UserId.Value);
        await notificationService.CreateNotificationAsync(
            userId,
            request.Title,
            request.Message,
            request.Type,
            request.ActionUrl,
            ct: ct);

        return ServiceResult.Success();
    }
}