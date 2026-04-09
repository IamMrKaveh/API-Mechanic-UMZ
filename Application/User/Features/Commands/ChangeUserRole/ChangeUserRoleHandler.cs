using Domain.User.Interfaces;

namespace Application.User.Features.Commands.ChangeUserRole;

public class ChangeUserRoleHandler(IUserRepository userRepository, IUnitOfWork unitOfWork) : IRequestHandler<ChangeUserRoleCommand, ServiceResult>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult> Handle(
        ChangeUserRoleCommand request,
        CancellationToken ct)
    {
        var user = await _userRepository.GetActiveByIdAsync(request.UserId, ct);

        if (user is null)
            return ServiceResult.NotFound("کاربر یافت نشد");

        if (user.Id == request.AdminUserId)
            return ServiceResult.Forbidden("امکان تغییر نقش خود وجود ندارد");

        user.SetAdminRole(request.IsAdmin);

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}