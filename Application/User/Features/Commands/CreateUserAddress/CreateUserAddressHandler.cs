using Application.User.Features.Shared;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.CreateUserAddress;

public class CreateUserAddressHandler(
    IUserRepository userRepository,
    ICurrentUserService currentUser,
    IMapper mapper)
    : ICommandHandler<CreateUserAddressCommand, UserAddressDto>
{
    public async Task<ServiceResult<UserAddressDto>> Handle(
        CreateUserAddressCommand request, CancellationToken ct)
    {
        var userId = UserId.From(currentUser.UserId!.Value);

        var userResult = await (userRepository.GetWithAddressesAsync(userId, ct)).OrNotFoundAsync("کاربر یافت نشد.");
        if (userResult.IsFailure) return userResult.Error;
        var user = userResult.Value;

        var phoneNumber = PhoneNumber.Create(request.PhoneNumber);
        var addressId = UserAddressId.NewId();

        var address = user.AddAddress(
            addressId,
            request.Title,
            request.ReceiverName,
            phoneNumber,
            request.Province,
            request.City,
            request.Address,
            request.PostalCode,
            request.Latitude,
            request.Longitude);

        if (request.IsDefault)
            user.SetDefaultAddress(addressId);

        userRepository.Update(user);

        return ServiceResult<UserAddressDto>.Success(mapper.Map<UserAddressDto>(address));
    }
}