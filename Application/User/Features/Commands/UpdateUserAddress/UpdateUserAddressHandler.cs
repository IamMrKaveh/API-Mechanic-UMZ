using Domain.User.Interfaces;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.UpdateUserAddress;

public class UpdateUserAddressHandler(IUserRepository userRepository, IUnitOfWork unitOfWork) : IRequestHandler<UpdateUserAddressCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        UpdateUserAddressCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);
        var addressId = UserAddressId.From(request.AddressId);
        var phoneNumber = PhoneNumber.Create(request.PhoneNumber);

        var user = await userRepository.GetWithAddressesAsync(userId, ct);
        if (user == null)
            return ServiceResult.NotFound("User not found.");
        user.UpdateAddress(
            addressId,
            request.Title,
            request.ReceiverName,
            phoneNumber,
            request.Province,
            request.City,
            request.Address,
            request.PostalCode,
            request.IsDefault,
            request.Latitude,
            request.Longitude);

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}