using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.UpdateUser;

public class UpdateUserHandler(
    IUserRepository userRepository)
    : ICommandHandler<UpdateUserCommand>
{
    public async Task<ServiceResult> Handle(
        UpdateUserCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.Id);

        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return ServiceResult.NotFound("کاربر یافت نشد.");

        if (!user.IsActive)
            return ServiceResult.Forbidden("حساب کاربری غیرفعال است و قابل ویرایش نیست.");

        var firstName = !string.IsNullOrEmpty(request.FirstName)
            ? request.FirstName
            : user.FullName.FirstName;

        var lastName = !string.IsNullOrEmpty(request.LastName)
            ? request.LastName
            : user.FullName.LastName;

        var fullName = FullName.Create(firstName, lastName);
        user.UpdateProfile(fullName, user.PhoneNumber);

        userRepository.Update(user);

        return ServiceResult.Success();
    }
}