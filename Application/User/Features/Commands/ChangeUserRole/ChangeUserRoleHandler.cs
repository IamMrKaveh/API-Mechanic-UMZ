using Application.Common.Models;
using Domain.User.Interfaces;

namespace Application.User.Features.Commands.ChangeUserRole;

public class ChangeUserRoleHandler : IRequestHandler<ChangeUserRoleCommand, ServiceResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeUserRoleHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> Handle(
        ChangeUserRoleCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetActiveByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return ServiceResult.Failure("کاربر یافت نشد");

        if (user.Id == request.AdminUserId)
            return ServiceResult.Failure("امکان تغییر نقش خود وجود ندارد");

        user.SetAdminRole(request.IsAdmin);

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }
}