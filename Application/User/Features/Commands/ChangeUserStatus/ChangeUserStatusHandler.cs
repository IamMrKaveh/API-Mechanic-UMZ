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

        var userResult = await (userRepository.GetByIdAsync(userId, ct)).OrNotFoundAsync("کاربر یافت نشد.");
        if (userResult.IsFailure) return userResult.Error;
        var user = userResult.Value;

        if (request.IsActive)
            user.Activate();
        else
            user.Deactivate();

        userRepository.Update(user);
        return ServiceResult.Success();
    }
}