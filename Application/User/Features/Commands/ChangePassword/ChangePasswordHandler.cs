using Application.Security.Interfaces;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.ChangePassword;

public class ChangePasswordHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangePasswordCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);

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