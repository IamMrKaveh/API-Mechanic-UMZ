using Application.User.Features.Shared;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.CreateUserAddress;

public class CreateUserAddressHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<CreateUserAddressCommand, ServiceResult<UserAddressDto>>
{
    public async Task<ServiceResult<UserAddressDto>> Handle(
        CreateUserAddressCommand request, CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);

        var user = await userRepository.GetWithAddressesAsync(userId, ct);
        if (user is null)
            return ServiceResult<UserAddressDto>.NotFound("کاربر یافت نشد.");

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
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<UserAddressDto>.Success(mapper.Map<UserAddressDto>(address));
    }
}