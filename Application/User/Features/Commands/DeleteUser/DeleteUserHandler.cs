using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.DeleteUser;

public class DeleteUserHandler(
    IUserRepository userRepository,
    ICurrentUserService currentUser)
    : ICommandHandler<DeleteUserCommand>
{
    public async Task<ServiceResult> Handle(
        DeleteUserCommand request,
        CancellationToken ct)
    {
        if (request.Id == currentUser.UserId!.Value)
            return ServiceResult.Forbidden("Admins cannot delete their own account this way.");

        var userId = UserId.From(request.Id);

        var userResult = await (userRepository.GetByIdAsync(userId)).OrNotFoundAsync("User Not Found");
        if (userResult.IsFailure) return userResult.Error;
        var user = userResult.Value;

        user.Deactivate();

        userRepository.Update(user);

        return ServiceResult.Success();
    }
}