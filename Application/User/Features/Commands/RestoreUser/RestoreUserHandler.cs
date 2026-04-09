using Domain.User.Interfaces;

namespace Application.User.Features.Commands.RestoreUser;

public class RestoreUserHandler(IUserRepository userRepository, IUnitOfWork unitOfWork) : IRequestHandler<RestoreUserCommand, ServiceResult>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult> Handle(
        RestoreUserCommand request,
        CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.Id);
        if (user == null)
            return ServiceResult.NotFound("User Not Found");

        user.Restore();

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}