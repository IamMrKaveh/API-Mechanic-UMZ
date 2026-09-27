using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.ChangeUserRole;

public class ChangeUserRoleHandler(
    IUserRepository userRepository,
    ICurrentUserService currentUser)
    : ICommandHandler<ChangeUserRoleCommand>
{
    public async Task<ServiceResult> Handle(
        ChangeUserRoleCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);
        var adminId = UserId.From(currentUser.UserId!.Value);

        var userResult = await (userRepository.GetActiveByIdAsync(userId, ct)).OrNotFoundAsync("کاربر یافت نشد");
        if (userResult.IsFailure) return userResult.Error;
        var user = userResult.Value;

        if (user.Id == adminId)
            return ServiceResult.Forbidden("امکان تغییر نقش خود وجود ندارد");

        if (request.IsAdmin)
            user.PromoteToAdmin();
        else
            user.DemoteFromAdmin();

        userRepository.Update(user);
        return ServiceResult.Success();
    }
}