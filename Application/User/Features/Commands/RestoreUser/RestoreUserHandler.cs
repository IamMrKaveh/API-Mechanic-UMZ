using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.RestoreUser;

public class RestoreUserHandler(IUserRepository userRepository, IUnitOfWork unitOfWork) : IRequestHandler<RestoreUserCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        RestoreUserCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.Id);

        var user = await userRepository.GetByIdAsync(userId);
        if (user is null)
            return ServiceResult.NotFound("User Not Found");

        user.Restore();

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}