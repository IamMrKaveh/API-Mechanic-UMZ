using Domain.User.Entities;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public interface ICheckoutAddressResolverService
{
    Task<ServiceResult<UserAddress>> ResolveAsync(
        int userId,
        int? userAddressId,
        CreateUserAddressDto? newAddress,
        bool saveNewAddress,
        CancellationToken ct);
}