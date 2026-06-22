using Application.Security.Contracts;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.ChangePassword;

public class ChangePasswordHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ChangePasswordCommand>
{
    public async Task<ServiceResult> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var userId = UserId.From(currentUser.UserId!.Value);

        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return ServiceResult.NotFound("کاربر یافت نشد.");

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return ServiceResult.Failure("رمز عبور فعلی نادرست است.");

        var newHash = passwordHasher.Hash(request.NewPassword);
        user.ChangePasswordHash(newHash);

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}