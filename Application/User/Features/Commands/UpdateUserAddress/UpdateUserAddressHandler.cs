using Domain.User.Interfaces;

namespace Application.User.Features.Commands.UpdateUserAddress;

public class UpdateUserAddressHandler(IUserRepository userRepository, IUnitOfWork unitOfWork) : IRequestHandler<UpdateUserAddressCommand, ServiceResult>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult> Handle(
        UpdateUserAddressCommand request,
        CancellationToken ct)
    {
        var user = await _userRepository.GetWithAddressesAsync(request.UserId, ct);
        if (user == null)
            return ServiceResult.NotFound("User not found.");
        user.UpdateAddress(
            request.AddressId,
            request.Dto.Title,
            request.Dto.ReceiverName,
            request.Dto.PhoneNumber,
            request.Dto.Province,
            request.Dto.City,
            request.Dto.Address.Address,
            request.Dto.PostalCode,
            request.Dto.IsDefault,
            request.Dto.Address.Latitude,
            request.Dto.Address.Longitude);

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}