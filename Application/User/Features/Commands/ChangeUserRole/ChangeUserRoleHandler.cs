using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.ChangeUserRole;

public class ChangeUserRoleHandler(IUserRepository userRepository, IUnitOfWork unitOfWork) : IRequestHandler<ChangeUserRoleCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        ChangeUserRoleCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);
        var adminId = UserId.From(request.AdminUserId);

        var user = await userRepository.GetActiveByIdAsync(userId, ct);

        if (user is null)
            return ServiceResult.NotFound("کاربر یافت نشد");

        if (user.Id == adminId)
            return ServiceResult.Forbidden("امکان تغییر نقش خود وجود ندارد");

        user.SetAdminRole(request.IsAdmin);

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}