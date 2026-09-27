using Application.Security.Contracts;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.ChangePassword;

public class ChangePasswordHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ICurrentUserService currentUser)
    : ICommandHandler<ChangePasswordCommand>
{
    public async Task<ServiceResult> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var userId = UserId.From(currentUser.UserId!.Value);

        var userResult = await (userRepository.GetByIdAsync(userId, ct)).OrNotFoundAsync("کاربر یافت نشد.");
        if (userResult.IsFailure) return userResult.Error;
        var user = userResult.Value;

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return ServiceResult.Failure("رمز عبور فعلی نادرست است.");

        var newHash = passwordHasher.Hash(request.NewPassword);
        user.ChangePasswordHash(newHash);

        userRepository.Update(user);

        return ServiceResult.Success();
    }
}