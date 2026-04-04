using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.User.Interfaces;

namespace Application.User.Features.Commands.ChangeUserStatus;

public class ChangeUserStatusHandler(IUserRepository userRepository, IUnitOfWork unitOfWork) : IRequestHandler<ChangeUserStatusCommand, ServiceResult>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult> Handle(
        ChangeUserStatusCommand request,
        CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.Id);
        if (user == null)
            return ServiceResult.NotFound("NotFound");

        user.SetIsActive(request.IsActive);
        _userRepository.Update(user);

        await _unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}