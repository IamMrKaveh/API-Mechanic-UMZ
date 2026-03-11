using Domain.User.Entities;
using Domain.User.Interfaces;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public sealed class CheckoutAddressResolverService(IUserRepository userRepository, IUnitOfWork unitOfWork) : ICheckoutAddressResolverService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult<UserAddress>> ResolveAsync(
        int userId,
        int? userAddressId,
        CreateUserAddressDto? newAddress,
        bool saveNewAddress,
        CancellationToken ct)
    {
        if (userAddressId.HasValue)
        {
            var existingAddress = await _userRepository.GetUserAddressAsync(userAddressId.Value, ct);
            if (existingAddress == null || existingAddress.UserId != userId)
                return ServiceResult<UserAddress>.Failure("آدرس انتخاب شده معتبر نیست.");
            return ServiceResult<UserAddress>.Success(existingAddress);
        }

        if (newAddress != null)
        {
            var createdAddress = UserAddress.Create(
                userId,
                newAddress.Title,
                newAddress.ReceiverName,
                newAddress.PhoneNumber,
                newAddress.Province,
                newAddress.City,
                newAddress.Address,
                newAddress.PostalCode,
                newAddress.IsDefault);

            if (saveNewAddress)
            {
                await _userRepository.AddAddressAsync(createdAddress, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }

            return ServiceResult<UserAddress>.Success(createdAddress);
        }

        return ServiceResult<UserAddress>.Failure("آدرس تحویل الزامی است.");
    }
}