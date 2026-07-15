using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.ChangeUserStatus;

public class ChangeUserStatusHandler(
    IUserRepository userRepository)
    : ICommandHandler<ChangeUserStatusCommand>
{
    public async Task<ServiceResult> Handle(
        ChangeUserStatusCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);

        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return ServiceResult.NotFound("کاربر یافت نشد.");

        if (request.IsActive)
            user.Activate();
        else
            user.Deactivate();

        userRepository.Update(user);
        return ServiceResult.Success();
    }
}