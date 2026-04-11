using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.ChangeUserStatus;

public class ChangeUserStatusHandler(IUserRepository userRepository, IUnitOfWork unitOfWork) : IRequestHandler<ChangeUserStatusCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        ChangeUserStatusCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);

        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
            return ServiceResult.NotFound("NotFound");

        user.SetIsActive(request.IsActive);
        userRepository.Update(user);

        await unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}