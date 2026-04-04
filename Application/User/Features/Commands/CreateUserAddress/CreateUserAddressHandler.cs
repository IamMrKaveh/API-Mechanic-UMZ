using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.User.Interfaces;

namespace Application.User.Features.Commands.CreateUserAddress;

public class CreateUserAddressHandler(IUserRepository userRepository, IUnitOfWork unitOfWork) : IRequestHandler<CreateUserAddressCommand, ServiceResult<int>>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult<int>> Handle(
        CreateUserAddressCommand request,
        CancellationToken ct)
    {
        var user = await _userRepository.GetWithAddressesAsync(request.UserId, ct);
        if (user is null)
            return ServiceResult<int>.NotFound("User not found.");
        var addr = user.AddAddress(request.Dto.Title, request.Dto.ReceiverName, request.Dto.PhoneNumber, request.Dto.Province, request.Dto.City, request.Dto.Address, request.Dto.PostalCode, request.Dto.IsDefault);
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(ct);
        return ServiceResult<int>.Success(addr.Id);
    }
}