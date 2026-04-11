using Application.User.Features.Shared;
using Domain.Common.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.CreateUser;

public class CreateUserHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<CreateUserCommand, ServiceResult<UserProfileDto>>
{
    public async Task<ServiceResult<UserProfileDto>> Handle(
        CreateUserCommand request,
        CancellationToken ct)
    {
        var phoneNumber = PhoneNumber.Create(request.PhoneNumber);
        var userId = UserId.NewId();
        var fullName = FullName.Create(request.FirstName, request.LastName);
        var email = Email.Create(request.Email);

        if (await userRepository.ExistsByPhoneNumberAsync(phoneNumber, null, ct))
            return ServiceResult<UserProfileDto>.Conflict("User with this phone number already exists.");

        var user = Domain.User.Aggregates.User.Create(
            userId,
            fullName,
            email,
            phoneNumber);

        await userRepository.AddAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = mapper.Map<UserProfileDto>(user);
        return ServiceResult<UserProfileDto>.Success(dto);
    }
}