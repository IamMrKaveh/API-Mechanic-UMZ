using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.RestoreUser;

public class RestoreUserHandler(
    IUserRepository userRepository)
    : ICommandHandler<RestoreUserCommand>
{
    public async Task<ServiceResult> Handle(
        RestoreUserCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.Id);

        var userResult = await (userRepository.GetByIdAsync(userId, ct)).OrNotFoundAsync("کاربر یافت نشد.");
        if (userResult.IsFailure) return userResult.Error;
        var user = userResult.Value;

        user.Activate();

        userRepository.Update(user);

        return ServiceResult.Success();
    }
}