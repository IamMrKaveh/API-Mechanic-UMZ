using Domain.User.Interfaces;

namespace Application.User.Features.Commands.ChangePassword;

public sealed class ChangePasswordHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangePasswordCommand, ServiceResult>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return ServiceResult.Failure("User not found.");

        if (!_passwordHasher.Verify(request.Dto.CurrentPassword, user.PasswordHash))
            return ServiceResult.Failure("Current password is incorrect.");

        user.ChangePassword(_passwordHasher.Hash(request.Dto.NewPassword));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }
}