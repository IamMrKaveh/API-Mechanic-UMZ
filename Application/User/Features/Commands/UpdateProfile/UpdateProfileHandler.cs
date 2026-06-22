using Application.User.Features.Shared;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.UpdateProfile;

public class UpdateProfileHandler(
    IUserRepository userRepository,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateProfileCommand, UserProfileDto>
{
    public async Task<ServiceResult<UserProfileDto>> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var userId = UserId.From(currentUser.UserId!.Value);

        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return ServiceResult<UserProfileDto>.NotFound("کاربر یافت نشد.");

        var fullName = FullName.Create(
            request.FirstName ?? user.FullName.FirstName,
            request.LastName ?? user.FullName.LastName);

        user.UpdateProfile(fullName, user.PhoneNumber);

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<UserProfileDto>.Success(user.Adapt<UserProfileDto>());
    }
}