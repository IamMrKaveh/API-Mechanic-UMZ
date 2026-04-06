using Application.Common.Results;
using Application.User.Features.Shared;
using Domain.Common.Interfaces;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.UpdateProfile;

public class UpdateProfileHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<UpdateProfileCommand, ServiceResult<UserProfileDto>>
{
    public async Task<ServiceResult<UserProfileDto>> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(UserId.From(request.UserId), ct);
        if (user is null)
            return ServiceResult<UserProfileDto>.NotFound("کاربر یافت نشد.");

        var fullName = FullName.Create(
            request.FirstName ?? user.FullName.FirstName,
            request.LastName ?? user.FullName.LastName);

        PhoneNumber? phoneNumber = null;
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            phoneNumber = PhoneNumber.Create(request.PhoneNumber);

        user.UpdateProfile(fullName, phoneNumber);

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<UserProfileDto>.Success(mapper.Map<UserProfileDto>(user));
    }
}